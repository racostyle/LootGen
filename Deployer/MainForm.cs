using System.Diagnostics;
using Deployer.Abstractions;
using Deployer.Infrastructure;
using Deployer.Models;
using Deployer.Services;
using Microsoft.Extensions.Logging;

namespace Deployer
{
    public sealed class MainForm : Form
    {
        private readonly IDeployService _deployService;
        private readonly IDeployerSettingsService _settingsService;
        private readonly IRepositoryLocator _repositoryLocator;
        private readonly IKeystoreService _keystoreService;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentInfo _environment;
        private readonly UiLogSink _logSink;
        private readonly ILogger<MainForm> _logger;

        private readonly RadioButton _windowsRadio = new();
        private readonly RadioButton _androidRadio = new();
        private readonly TextBox _displayVersionBox = new();
        private readonly NumericUpDown _versionCodeBox = new();
        private readonly TextBox _repositoryBox = new();
        private readonly TextBox _outputBox = new();
        private readonly TextBox _keystoreBox = new();
        private readonly TextBox _aliasBox = new();
        private readonly TextBox _passwordBox = new();
        private readonly GroupBox _androidGroup = new();
        private readonly Button _buildButton = new();
        private readonly Button _cancelButton = new();
        private readonly Button _openOutputButton = new();
        private readonly TextBox _logBox = new();

        private CancellationTokenSource? _runCancellation;

        public MainForm(
            IDeployService deployService,
            IDeployerSettingsService settingsService,
            IRepositoryLocator repositoryLocator,
            IKeystoreService keystoreService,
            IFileSystem fileSystem,
            IEnvironmentInfo environment,
            UiLogSink logSink,
            ILogger<MainForm> logger)
        {
            _deployService = deployService;
            _settingsService = settingsService;
            _repositoryLocator = repositoryLocator;
            _keystoreService = keystoreService;
            _fileSystem = fileSystem;
            _environment = environment;
            _logSink = logSink;
            _logger = logger;

            Text = "LootGen Deployer";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(880, 720);
            Size = new Size(960, 780);
            Font = new Font("Segoe UI", 9.75f);
            AutoScaleMode = AutoScaleMode.Dpi;

            BuildLayout();
            LoadSettings();
            _logSink.Writer = AppendLog;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _logSink.Writer = null;
            SaveSettings();
            _runCancellation?.Dispose();
            base.OnFormClosed(e);
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(16)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(CreateHeader(), 0, 0);
            root.Controls.Add(CreateTargetGroup(), 0, 1);
            root.Controls.Add(CreateVersionAndPaths(), 0, 2);
            root.Controls.Add(CreateAndroidGroup(), 0, 3);
            root.Controls.Add(CreateButtons(), 0, 4);
            root.Controls.Add(CreateLogBox(), 0, 5);
            Controls.Add(root);
        }

        private static Control CreateHeader()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            panel.Controls.Add(new Label
            {
                Text = "Build GitHub release packages",
                AutoSize = true,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            });
            panel.Controls.Add(new Label
            {
                Text = "Windows produces a self-contained zip (unzip and run). Android produces a signed APK for sideloading — not Play Store.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 0, 0, 12)
            });
            return panel;
        }

        private Control CreateTargetGroup()
        {
            var group = new GroupBox
            {
                Text = "Package",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(12)
            };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            _windowsRadio.Text = "Windows package (.zip)  —  attach to a GitHub Release; recipients unzip and run LootGen.exe";
            _windowsRadio.AutoSize = true;
            _windowsRadio.Checked = true;
            _windowsRadio.CheckedChanged += (_, _) => UpdateAndroidEnabled();

            _androidRadio.Text = "Android APK  —  attach to a GitHub Release; recipients allow install from unknown sources";
            _androidRadio.AutoSize = true;

            flow.Controls.Add(_windowsRadio);
            flow.Controls.Add(_androidRadio);
            group.Controls.Add(flow);
            return group;
        }

        private Control CreateVersionAndPaths()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(0, 8, 0, 8)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

            _displayVersionBox.Dock = DockStyle.Fill;
            _versionCodeBox.Dock = DockStyle.Fill;
            _versionCodeBox.Minimum = 1;
            _versionCodeBox.Maximum = 2_000_000_000;
            _versionCodeBox.Value = 1;
            _repositoryBox.Dock = DockStyle.Fill;
            _outputBox.Dock = DockStyle.Fill;

            AddLabeledRow(table, 0, "Display version", _displayVersionBox, null);
            AddLabeledRow(table, 1, "Version code", _versionCodeBox, null);
            AddLabeledRow(table, 2, "Repository root", _repositoryBox, CreateBrowseFolderButton(() => _repositoryBox));
            AddLabeledRow(table, 3, "Output folder", _outputBox, CreateBrowseFolderButton(() => _outputBox));
            return table;
        }

        private Control CreateAndroidGroup()
        {
            _androidGroup.Text = "Android signing (required for GitHub sideload APKs)";
            _androidGroup.Dock = DockStyle.Fill;
            _androidGroup.AutoSize = true;
            _androidGroup.Padding = new Padding(12);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 4
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

            _keystoreBox.Dock = DockStyle.Fill;
            _aliasBox.Dock = DockStyle.Fill;
            _passwordBox.Dock = DockStyle.Fill;
            _passwordBox.UseSystemPasswordChar = true;
            _aliasBox.Text = PublishConstants.DefaultKeystoreAlias;

            var browseKeystore = new Button { Text = "Browse...", Dock = DockStyle.Fill };
            browseKeystore.Click += (_, _) => BrowseKeystore();
            var createKeystore = new Button { Text = "Create", Dock = DockStyle.Fill };
            createKeystore.Click += async (_, _) => await CreateKeystoreAsync();

            AddLabeledRow(table, 0, "Keystore", _keystoreBox, browseKeystore);
            AddLabeledRow(table, 1, "Alias", _aliasBox, createKeystore);
            AddLabeledRow(table, 2, "Password", _passwordBox, null);

            var hint = new Label
            {
                Text = "Keep this keystore. The same key is required to update an already installed APK.",
                AutoSize = true,
                ForeColor = Color.DimGray
            };
            table.SetColumnSpan(hint, 3);
            table.Controls.Add(hint, 0, 3);

            _androidGroup.Controls.Add(table);
            UpdateAndroidEnabled();
            return _androidGroup;
        }

        private Control CreateButtons()
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 8, 0, 8)
            };

            _buildButton.Text = "Build package";
            _buildButton.AutoSize = true;
            _buildButton.Padding = new Padding(16, 6, 16, 6);
            _buildButton.Click += async (_, _) => await BuildAsync();

            _cancelButton.Text = "Cancel";
            _cancelButton.AutoSize = true;
            _cancelButton.Enabled = false;
            _cancelButton.Click += (_, _) => _runCancellation?.Cancel();

            _openOutputButton.Text = "Open output folder";
            _openOutputButton.AutoSize = true;
            _openOutputButton.Click += (_, _) => OpenOutputFolder();

            flow.Controls.Add(_buildButton);
            flow.Controls.Add(_cancelButton);
            flow.Controls.Add(_openOutputButton);
            return flow;
        }

        private Control CreateLogBox()
        {
            _logBox.Dock = DockStyle.Fill;
            _logBox.Multiline = true;
            _logBox.ReadOnly = true;
            _logBox.ScrollBars = ScrollBars.Both;
            _logBox.WordWrap = false;
            _logBox.Font = new Font("Consolas", 9f);
            return _logBox;
        }

        private static void AddLabeledRow(TableLayoutPanel table, int row, string label, Control field, Control? extra)
        {
            table.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);
            table.Controls.Add(field, 1, row);
            if (extra is not null)
            {
                table.Controls.Add(extra, 2, row);
            }
        }

        private Button CreateBrowseFolderButton(Func<TextBox> target)
        {
            var button = new Button
            {
                Text = "Browse...",
                Dock = DockStyle.Fill
            };
            button.Click += (_, _) =>
            {
                using var dialog = new FolderBrowserDialog
                {
                    SelectedPath = target().Text
                };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    target().Text = dialog.SelectedPath;
                }
            };
            return button;
        }

        private void BrowseKeystore()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Keystore files (*.keystore;*.jks)|*.keystore;*.jks|All files (*.*)|*.*",
                FileName = _keystoreBox.Text
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _keystoreBox.Text = dialog.FileName;
            }
        }

        private async Task CreateKeystoreAsync()
        {
            var path = string.IsNullOrWhiteSpace(_keystoreBox.Text)
                ? _settingsService.GetDefaultKeystorePath()
                : _keystoreBox.Text.Trim();
            var alias = string.IsNullOrWhiteSpace(_aliasBox.Text)
                ? PublishConstants.DefaultKeystoreAlias
                : _aliasBox.Text.Trim();
            var password = _passwordBox.Text;
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                MessageBox.Show(this, "Enter a keystore password of at least 6 characters before creating the keystore.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetBusy(true);
                await _keystoreService.CreateAsync(new SKeystoreCreateRequest
                {
                    KeystorePath = path,
                    Alias = alias,
                    Password = password,
                    DistinguishedName = "CN=LootGen, OU=LootGen, O=LootGen, L=Unknown, ST=Unknown, C=US"
                }, CancellationToken.None);
                _keystoreBox.Text = path;
                _aliasBox.Text = alias;
                MessageBox.Show(this, $"Keystore created at:{Environment.NewLine}{path}{Environment.NewLine}{Environment.NewLine}Back it up. Losing it means you cannot update the same installed APK.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create keystore");
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task BuildAsync()
        {
            SaveSettings();
            _runCancellation?.Dispose();
            _runCancellation = new CancellationTokenSource();
            SetBusy(true);
            try
            {
                var request = new SDeployRequest
                {
                    Target = _androidRadio.Checked ? DeployTarget.AndroidApk : DeployTarget.WindowsPackage,
                    DisplayVersion = _displayVersionBox.Text.Trim(),
                    ApplicationVersion = (int)_versionCodeBox.Value,
                    RepositoryRoot = _repositoryBox.Text.Trim(),
                    OutputDirectory = _outputBox.Text.Trim(),
                    KeystorePath = _keystoreBox.Text.Trim(),
                    KeystoreAlias = _aliasBox.Text.Trim(),
                    KeystorePassword = _passwordBox.Text,
                    KeyPassword = _passwordBox.Text
                };

                var result = await _deployService.DeployAsync(request, _runCancellation.Token);
                if (result.Succeeded)
                {
                    MessageBox.Show(this, "Completed.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, result.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Deploy cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deploy failed");
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void OpenOutputFolder()
        {
            var path = _outputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            _fileSystem.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private void SetBusy(bool busy)
        {
            _buildButton.Enabled = !busy;
            _cancelButton.Enabled = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void UpdateAndroidEnabled()
        {
            _androidGroup.Enabled = _androidRadio.Checked;
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Load();
            _displayVersionBox.Text = settings.DisplayVersion;
            _versionCodeBox.Value = Math.Max(1, settings.ApplicationVersion);
            _androidRadio.Checked = settings.LastTarget == DeployTarget.AndroidApk;
            _windowsRadio.Checked = !_androidRadio.Checked;
            _aliasBox.Text = string.IsNullOrWhiteSpace(settings.KeystoreAlias)
                ? PublishConstants.DefaultKeystoreAlias
                : settings.KeystoreAlias;
            _keystoreBox.Text = string.IsNullOrWhiteSpace(settings.KeystorePath)
                ? _settingsService.GetDefaultKeystorePath()
                : settings.KeystorePath;

            if (!string.IsNullOrWhiteSpace(settings.RepositoryRoot)
                && _fileSystem.FileExists(_fileSystem.Combine(settings.RepositoryRoot, "GUI", "GUI.csproj")))
            {
                _repositoryBox.Text = settings.RepositoryRoot;
            }
            else if (_repositoryLocator.TryFindRoot(_environment.ApplicationBaseDirectory, out var fromBase)
                     || _repositoryLocator.TryFindRoot(_environment.CurrentDirectory, out fromBase))
            {
                _repositoryBox.Text = fromBase;
            }

            _outputBox.Text = string.IsNullOrWhiteSpace(settings.OutputDirectory)
                ? _settingsService.GetDefaultOutputDirectory(_repositoryBox.Text)
                : settings.OutputDirectory;

            UpdateAndroidEnabled();
        }

        private void SaveSettings()
        {
            _settingsService.Save(new DeployerSettings
            {
                DisplayVersion = _displayVersionBox.Text.Trim(),
                ApplicationVersion = (int)_versionCodeBox.Value,
                RepositoryRoot = _repositoryBox.Text.Trim(),
                OutputDirectory = _outputBox.Text.Trim(),
                KeystorePath = _keystoreBox.Text.Trim(),
                KeystoreAlias = _aliasBox.Text.Trim(),
                LastTarget = _androidRadio.Checked ? DeployTarget.AndroidApk : DeployTarget.WindowsPackage
            });
        }

        private void AppendLog(string message)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(AppendLog, message);
                return;
            }

            _logBox.AppendText(message + Environment.NewLine);
        }
    }
}

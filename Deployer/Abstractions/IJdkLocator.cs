namespace Deployer.Abstractions
{
    public interface IJdkLocator
    {
        bool TryFindKeytool(out string keytoolPath);
    }
}

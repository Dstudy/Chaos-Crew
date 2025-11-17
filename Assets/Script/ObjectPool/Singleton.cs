public class Singleton<T> where T : class
{
    private static T _instance;

    public static T Instance => _instance ??= Create();
    
    public static T Create()
    {
        return _instance ??= System.Activator.CreateInstance<T>();
    }
}
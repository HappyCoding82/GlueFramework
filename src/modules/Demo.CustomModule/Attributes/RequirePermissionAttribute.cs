namespace Demo.CustomModule.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute
    {
        public string PermissionName { get; }

        public RequirePermissionAttribute(string permissionName)
        {
            PermissionName = permissionName;
        }
    }
}
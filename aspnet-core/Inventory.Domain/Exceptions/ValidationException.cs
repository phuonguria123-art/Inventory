namespace Inventory.Domain.Exceptions
{
    /// <summary>
    /// khi dữ liệu sai / mã trùng
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}

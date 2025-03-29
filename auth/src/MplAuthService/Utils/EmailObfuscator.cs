namespace MplAuthService.Utils
{
    public static class EmailObfuscator
    {
        public static string? ObfuscateEmail(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return email;
            }
            int atIndex = email.IndexOf('@');
            if(atIndex <= 1)
            {
                return email;
            }
            string localPart = email.Substring(0, 1);
            string domainPart = email.Substring(atIndex);
            return $"{localPart}***{domainPart}";
        }
    }
}
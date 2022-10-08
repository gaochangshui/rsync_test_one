using System;

namespace GitlabManager.Models
{
    public class Entities
    {
    }

    public class LoginRequest
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    public class AuthInfo
    {
        public string UserId { get; set; }
        public DateTime Expires { get; set; }
    }

    public class HttpResult
    {
        public bool Success { get; set; }
        public dynamic Data { get; set; }
        public string Message { get; set; }
    }
}
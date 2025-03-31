﻿namespace BaseIdentity.Application.DTOs.Request
{
    public class JwtSettings
    {
        public string? Key { get; set; }
        public string ValidIssuer { get; set; } = string.Empty;
        public string ValidAudience { get; set; } = string.Empty;
        public double Expires { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class GoogleExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PizzeriaApp.GoogleAuth
{
    public class GoogleUserDTO
    {
        public string TokenId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
    }
}

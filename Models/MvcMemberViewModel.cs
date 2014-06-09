using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Umbraco714.Models
{
    public class MvcMemberModel
    {
        // for login
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }

    public class MvcMemberRegisterModel : MvcMemberModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool MailingListInclude { get; set; }
    }

    public class MvcMemberValidateModel
    {
        public string Email { get; set; }
        public string ValidateGUID { get; set; }
    }

}
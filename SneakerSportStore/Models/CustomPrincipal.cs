using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SneakerSportStore.Models
{
    public class CustomPrincipal : IPrincipal
    {
        private readonly IIdentity _identity;
        public string[] Roles { get; set; }

        public CustomPrincipal(IIdentity identity)
        {
            _identity = identity;
        }

        public IIdentity Identity => _identity;

        public bool IsInRole(string role)
        {
            return Roles != null && Roles.Contains(role);
        }
    }
}
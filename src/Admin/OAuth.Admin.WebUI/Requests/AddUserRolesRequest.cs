using OAuth.Admin.WebUI.ViewModels;
﻿
using System.Collections.Generic;

namespace OAuth.Admin.WebUI.Requests
{
    public class AddUserRolesRequest
    {
        public string UserName { get; set; } = string.Empty;

        public List<UserRoleViewModel> RolesToAdd { get; set; } = [];

        public AddUserRolesRequest()
        {

        }

        public AddUserRolesRequest(string userName, IEnumerable<UserRoleViewModel> rolesToAdd)
        {
            this.UserName = userName;
            this.RolesToAdd.AddRange(rolesToAdd);
        }

    }
}

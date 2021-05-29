using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Microsoft.AspNetCore.Mvc 
{
    public static class ERKExtensionsController
    {
        public static int GetMyId(this Controller controller) => int.Parse(controller.User.Identity.Name);
    }
}

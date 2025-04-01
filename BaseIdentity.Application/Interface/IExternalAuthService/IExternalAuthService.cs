using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthenticationApi.Application.Common;

namespace BaseIdentity.Application.Interface.IExternalAuthService
{
    public interface IExternalAuthService
    {
        Task<ApiResult<string>> ProcessGoogleLoginAsync();

    }
}

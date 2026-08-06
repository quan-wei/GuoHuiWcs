using Guohui_Wcs.Models;
using Microsoft.AspNetCore.Mvc;

namespace Guohui_Wcs.Interfaces
{
    public interface IReportController
    {
        IActionResult Index();

        Task<IActionResult> UpdateGlobalVariableAsync(RobotTaskNotification jsonData);
    }
}

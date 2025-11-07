using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ConditionalRefreshService
{
    /// <summary>
    /// Demonstrates conditional refresh - only refresh if certain conditions are met
    /// </summary>
    public interface IConditionalRefreshService
    {
        Task<string> GetDataWithConditionalRefreshAsync(string key);
    }
}

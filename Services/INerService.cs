using System.Collections.Generic;
using NerApi.Models;

namespace NerApi.Services
{
    public interface INerService
    {
        List<NerEntity> ExtractEntities(string text);
    }
}
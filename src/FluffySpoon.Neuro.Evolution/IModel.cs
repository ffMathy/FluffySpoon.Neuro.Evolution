using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IModel<TModel> where TModel : IModel<TModel>
    {
        Task ResetAsync();
        Task<TModel> CreateNew();
    }
}

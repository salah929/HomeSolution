using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home.Services.Exceptions
{
    // sealed means “no one should inherit from this.”
    public sealed class DomainValidationException : Exception
    {
        public IReadOnlyList<(string Key, string Message)> Errors { get; }
        public DomainValidationException(IEnumerable<(string Key, string Message)> errors)
            : base("Validation failed") => Errors = errors.ToList();
    }

}

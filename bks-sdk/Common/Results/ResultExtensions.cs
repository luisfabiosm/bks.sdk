using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Results
{
    public static class ResultExtensions
    {
        public static Result<T> Success<T>(T value) => new(true, value, null);
        public static Result<T> Failure<T>(string error) => new(false, default, error);
    }
}

using Kleios.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Kleios.Backend.Shared
{
    public class Result : ObjectResult
    {
        public Result(object value) : base(value)
        {

        }

        public static implicit operator Result(Option option)
        {
            if (option.IsFailure)
            {
                return new Result(option.Message)
                {
                    StatusCode = (int)option.StatusCode
                };
            }
            return new Result(option)
            {
                StatusCode = (int)option.StatusCode
            };

        }
    }
}
using Kleios.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Kleios.Backend.Shared
{


    public class Result<T> : ObjectResult
    {
        public Result(object? value) : base(value)
        {

        }

        public static implicit operator Result<T>(Option<T> option)
        {
            if (option.IsFailure)
            {
                return new Result<T>(option.Message)
                {
                    StatusCode = (int)option.StatusCode
                };
            }
            return new Result<T>(option.Value)
            {
                StatusCode = (int)option.StatusCode
            };

        }
    }
}


using System.Text;

namespace Foodtrucks.Api.Commands
{
    public class CommandResult
    {
        
        public bool IsSuccess => Success;
        public bool Success { get; set; }
        public int? Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsNotFound { get; set; } = false;
        public bool IsForbidden { get; set; } = false;

        internal static CommandResult Failure(string message)
        {
            return new CommandResult
            {
                Success = false,
                Message = message
            };
        }

        internal static CommandResult ForbiddenResult(string message = "Forbidden")
        {
            return new CommandResult
            {
                Success = false,
                Message = message,
                IsForbidden = true
            };
        }

        internal static CommandResult NotFoundResult()
        {
            return new CommandResult
            {
                Success = false,
                Message = "Not Found",
                IsNotFound = true
            };
        }

        internal static CommandResult NotFoundResult(string message)
        {
            return new CommandResult
            {
                Success = false,
                Message = message,
                IsNotFound = true
            };
        }


        internal static CommandResult Failure(IDictionary<string, string[]> validationErrors)
        {
            var sb = new StringBuilder();
            foreach (var key in validationErrors.Keys)
            {
                var messages = string.Join(", ", validationErrors[key]);
                sb.AppendLine($"{key}: {messages}");
            }
            return new CommandResult
            {
                Success = false,
                Message = sb.ToString().Trim()
            };
        }

        internal static CommandResult SuccessResult(int? id, string message)
        {
            return new CommandResult
            {
                Success = true,
                Message = message,
                Id = id
            };
        }

        internal static CommandResult SuccessResult()
        {
            return new CommandResult
            {
                Success = true,
            
            };
        }
    }

    public class CommandResult<T> : CommandResult
    {
        public T? Data { get; set; }

        internal static new CommandResult<T> Failure(string message)
        {
            return new CommandResult<T>
            {
                Success = false,
                Message = message
            };
        }

        internal static new CommandResult<T> Failure(IDictionary<string, string[]> validationErrors)
        {
            var sb = new StringBuilder();
            foreach (var key in validationErrors.Keys)
            {
                var messages = string.Join(", ", validationErrors[key]);
                sb.AppendLine($"{key}: {messages}");
            }
            return new CommandResult<T>
            {
                Success = false,
                Message = sb.ToString().Trim()
            };
        }

        internal static new CommandResult<T> NotFoundResult(string message = "Not Found")
        {
             return new CommandResult<T>
            {
                Success = false,
                Message = message,
                IsNotFound = true
            };
        }

        internal static CommandResult<T> SuccessResult(T data)
        {
            return new CommandResult<T>
            {
                Success = true,
                Data = data
            };
        }
    }
}

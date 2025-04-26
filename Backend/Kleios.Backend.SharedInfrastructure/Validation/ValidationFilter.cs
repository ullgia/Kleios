using System.Net;
using FluentValidation;
using Kleios.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kleios.Backend.SharedInfrastructure.Validation;

/// <summary>
/// Filtro di azione per la validazione delle richieste utilizzando FluentValidation.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Se l'azione non ha parametri, procediamo
        if (!context.ActionArguments.Any())
        {
            await next();
            return;
        }

        // Validazione degli argomenti dell'azione
        foreach (var (key, value) in context.ActionArguments)
        {
            if (value == null) continue;
            
            // Troviamo il tipo di validatore specifico per questo argomento
            var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
            
            // Otteniamo il validatore specifico dal container di DI
            var validator = _serviceProvider.GetService(validatorType);
            
            // Se non c'è un validatore per questo tipo, continuiamo
            if (validator == null) continue;
            
            // Utilizziamo ValidatorExtensions per creare un contesto di validazione appropriato
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(value.GetType());
            var validationContext = Activator.CreateInstance(validationContextType, value);
            
            // Utilizziamo reflection per chiamare il metodo ValidateAsync
            var validateMethod = validatorType.GetMethod("ValidateAsync", new[] { validationContextType, typeof(CancellationToken) });
            if (validateMethod == null) continue;
            
            // Invochiamo il metodo ValidateAsync sul validatore
            var validationResultTask = validateMethod.Invoke(validator, new[] { validationContext, context.HttpContext.RequestAborted });
            
            // Aspettiamo il completamento del task
            if (validationResultTask is Task task)
            {
                await task;
                
                // Otteniamo il risultato dal task completato
                var resultProperty = task.GetType().GetProperty("Result");
                var validationResult = resultProperty?.GetValue(task);
                
                // Verifichiamo se il risultato è valido
                if (validationResult != null)
                {
                    var isValidProperty = validationResult.GetType().GetProperty("IsValid");
                    var isValid = (bool)(isValidProperty?.GetValue(validationResult) ?? true);
                    
                    if (!isValid)
                    {
                        // Otteniamo gli errori
                        var errorsProperty = validationResult.GetType().GetProperty("Errors");
                        var errors = errorsProperty?.GetValue(validationResult) as System.Collections.IEnumerable;
                        
                        if (errors != null)
                        {
                            var errorMessages = new List<string>();
                            foreach (var error in errors)
                            {
                                var errorMessageProperty = error.GetType().GetProperty("ErrorMessage");
                                var errorMessage = errorMessageProperty?.GetValue(error) as string;
                                if (!string.IsNullOrEmpty(errorMessage))
                                {
                                    errorMessages.Add(errorMessage);
                                }
                            }
                            
                            if (errorMessages.Any())
                            {
                                var errorMessage = string.Join("; ", errorMessages);
                                
                                // Creiamo un risultato di fallimento
                                var result = Option.Failure(errorMessage);
                                
                                // Impostiamo il risultato e interrompiamo l'esecuzione
                                context.Result = new ObjectResult(result)
                                {
                                    StatusCode = (int)HttpStatusCode.BadRequest
                                };
                                
                                return;
                            }
                        }
                    }
                }
            }
        }
        
        // Se tutti i validatori passano, procediamo con l'esecuzione dell'azione
        await next();
    }
}
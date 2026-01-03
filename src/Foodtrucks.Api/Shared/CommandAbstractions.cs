namespace Foodtrucks.Api.Shared
{
    public interface ICommand<out TResult>
    {
    }

    public interface ICommandHandler<in TCommand, TResult> 
        where TCommand : ICommand<TResult>
    {
        Task<TResult> Handle(TCommand command, CancellationToken ct);
    }
}

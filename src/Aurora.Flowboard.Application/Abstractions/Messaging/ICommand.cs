namespace Aurora.Flowboard.Application.Abstractions.Messaging;

public interface ICommand;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface ICommand<TResponse>;
#pragma warning restore S2326 // Unused type parameters should be removed

public interface IBaseCommand;

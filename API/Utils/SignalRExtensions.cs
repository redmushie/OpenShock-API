﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using StackExchange.Redis;

namespace OpenShock.API.Utils;

public static class SignalRExtensions
{
    /// <summary>
    /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared Redis server.
    /// </summary>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="redisConnectionString">The connection string used to connect to the Redis server.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddOpenShockStackExchangeRedis(this ISignalRServerBuilder signalrBuilder, string redisConnectionString)
    {
        return AddOpenShockStackExchangeRedis(signalrBuilder, o =>
        {
            o.Configuration = ConfigurationOptions.Parse(redisConnectionString);
        });
    }
    
    /// <summary>
    /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared Redis server.
    /// </summary>
    /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
    /// <param name="configure">A callback to configure the Redis options.</param>
    /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
    public static ISignalRServerBuilder AddOpenShockStackExchangeRedis(this ISignalRServerBuilder signalrBuilder, Action<RedisOptions> configure)
    {
        signalrBuilder.Services.Configure(configure);
        signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OpenShockRedisHubLifetimeManager<>));
        return signalrBuilder;
    }
}
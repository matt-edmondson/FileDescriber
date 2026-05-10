// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.Collections.Generic;
using System.Threading;

// Distributes requests across a pool of endpoints using least-connections balancing.
// Each new request is routed to the endpoint that currently has the fewest in-flight
// requests, so a slow endpoint cannot monopolise the concurrency budget.
internal sealed class EndpointLoadBalancer
{
	private readonly IReadOnlyList<OllamaEndpoint> _endpoints;
	private readonly int[] _activeCounts;

	internal EndpointLoadBalancer(IReadOnlyList<OllamaEndpoint> endpoints)
	{
		_endpoints = endpoints;
		_activeCounts = new int[endpoints.Count];
	}

	// Returns the index of the least-loaded endpoint and increments its active count.
	// Call Release with the same index when the request completes.
	internal int Acquire()
	{
		int minIndex = 0;
		int minCount = Volatile.Read(ref _activeCounts[0]);

		for (int i = 1; i < _activeCounts.Length; i++)
		{
			int count = Volatile.Read(ref _activeCounts[i]);
			if (count < minCount)
			{
				minCount = count;
				minIndex = i;
			}
		}

		Interlocked.Increment(ref _activeCounts[minIndex]);
		return minIndex;
	}

	// Decrements the active count for the endpoint at the given index.
	// Must be called exactly once for every successful Acquire call.
	internal void Release(int index) => Interlocked.Decrement(ref _activeCounts[index]);

	internal OllamaEndpoint this[int index] => _endpoints[index];
}

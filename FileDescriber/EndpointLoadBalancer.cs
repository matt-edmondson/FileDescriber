// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.FileDescriber;

using System.Collections.Generic;

// Distributes requests across a pool of endpoints using least-connections balancing.
// Each new request is routed to the endpoint that currently has the fewest in-flight
// requests, so a slow endpoint cannot monopolise the concurrency budget.
internal sealed class EndpointLoadBalancer
{
	private readonly IReadOnlyList<AiEndpoint> _endpoints;
	private readonly int[] _activeCounts;
	private readonly Lock _lock = new();

	internal EndpointLoadBalancer(IReadOnlyList<AiEndpoint> endpoints)
	{
		_endpoints = endpoints;
		_activeCounts = new int[endpoints.Count];
	}

	// Returns the index and endpoint of the least-loaded endpoint, atomically incrementing its active count.
	// Call Release with the returned index when the request completes.
	internal (int Index, AiEndpoint Endpoint) Acquire()
	{
		lock (_lock)
		{
			int minIndex = 0;
			int minCount = _activeCounts[0];

			for (int i = 1; i < _activeCounts.Length; i++)
			{
				if (_activeCounts[i] < minCount)
				{
					minCount = _activeCounts[i];
					minIndex = i;
				}
			}

			_activeCounts[minIndex]++;
			return (minIndex, _endpoints[minIndex]);
		}
	}

	// Decrements the active count for the endpoint at the given index.
	// Must be called exactly once for every successful Acquire call.
	internal void Release(int index)
	{
		lock (_lock)
		{
			if (_activeCounts[index] > 0)
			{
				_activeCounts[index]--;
			}
		}
	}

	internal AiEndpoint this[int index] => _endpoints[index];
}

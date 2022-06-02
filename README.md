# CicuitBreaker

Note: It should not have to be said, but the following is an experimental prototype of a Circuit breaker pattern. Under no circumstance should it be used in any production code. Use a mature and resilient solution, like Polly.


A Circuit Breaker is a construct designed to manage access to resources, and prevent cascading failures to build up in a distributed system. It works by monitoring faults in the system and artificially throttling processes when pieces begin failing. The state can be closed, open or partially open, representing how well the system is performing.

What is here:

A simple state machine with a state that is synchronized among processes running on a local machine. The circuit breaker will pass around lambda functions, monitoring exceptions. Failures are incremented, and will trip the circuit when enough are accumulated. There is no timeout reset for failures, but should be implemented. There is a timeout for when a failure will begin to partially open (see: half-open). The half-open state will throttle requests to force all processes onto a single thread per action.  A single exception will force a half-open circuit back open. After enough success actions have been run through the circuit breaker, the logic will determine the faulting issues passed and will close the circuit allowing concurrent actions again.

A simulated service. There’s a console application that will spin up multiple threads, and send requests to our web service (described below) indefinitely. A circuit breaker, that will manage access to the web service. If enough failures occur, the circuit will trip, and will not allow any requests through. There is a failure timeout before the circuit partially opens. 

When the circuit breaker first throws an error, our local service will switch to a single thread retry attempt, with an exponential back-off. It will occasionally check if the circuit breaker is available. 

Managing Service: Creates a synchronized state in memory that can be synchronized across multiple services. Additionally, this management service will periodically check the overall status of the circuit state, and will open/close/half-close based on the configuration. Process should be running for the other console apps to run.

Web Service:
A simple web api that fakes a queue process. Each request is added to a queue, and then popped off when the request is over. Occasionally, the controller will throw an exception, simulating an unreliable endpoint. An http error code will return only some times, but it will not clear the queue. Eventually, the service queue will fill up entirely, and the service will become unreliable for all requests. This will simulate a complete failure that will need a manual reset. Otherwise, our local service will forever fail at the circuit breaker.

Missing/To Do:
Unit Tests 
Containerize and Synchronize State management
clean up concurrency
Comments

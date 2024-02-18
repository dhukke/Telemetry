# Telemetry

Playing around with open telemetry.

Here we have an API that send message that is consumed by Worker and we collect the traces, we can see the traces in zipkin.

The main goal of this project was to see this in [zipkin](http://127.0.0.1:9411/zipkin):

![alt text for screen readers](/docs/images/zipkin-trace.png "Text to show on mouseover")

## How to run

We can run it via Visual Studio 2022, then open [swagger](https://localhost:52443/swagger/index.html), there we have only 1 endpoint, just make a call to the endpoint than access [zipkin](http://127.0.0.1:9411/zipkin) to run a query and it should show the traces.
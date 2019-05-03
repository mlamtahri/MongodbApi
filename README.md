# FDNS Object Microservice: A REST backing service for database operations

"Object" is a backing service with a simple RESTful API for database operations. Supported operations include CRUD, search, data pipelines, aggregation, and bulk imports.

Backing services are [factor #4 of the 12-factor app methodology](https://12factor.net/backing-services). Applied to database operations, the result is that virtually all languages and platforms can be used to interact with the database. These includes languages that don't have a Mongo SDK, like Rust, SAS, and R. One can even use a terminal session with `curl`. A deploy of the Object microservice is able to swap out a local Mongo database with one managed by a third party (such as Azure CosmosDB, a managed MongoDB cluster on AWS, or an on-prem Mongo cluster) without any source code changes. 

The Object backing service makes it unnecessary to learn or implement a Mongo SDK to interact with the organization's persistent storage. This is a benefit for data scientists and analysts who are unlikely to have strong software engineering backgrounds. Conversely, analysts and data scientists ought to be very familiar with retreiving data from HTTP API endpoints.

An additional benefit is centralized data access, authentication, and authorization. The Object backing service is secured via OAuth2 using scope-based authorization. Data scientists thus have a known, consistent way of requesting access to and retreiving data for the entire organization. While HTTP APIs for the organization's data can still be implemented without the Object backing service, they will be scattered across many URLs and are unlikely to have consistent APIs.

If the microservice becomes unhealthy or unresponsive, it can be (depending on which container orchestrator is used) automatically killed and restarted with little delay. Being a microservice means extremely efficient use of computing resources, making these kill-restart cycles very quick. This efficiency also means that scaling the backing service to meet spikes in demand can be done fast and seamlessly.

> This repository represents an unofficial re-implementation of the U.S. Centers for Disease Control and Prevention's [Object microservice](https://github.com/CDCgov/fdns-ms-object) using [ASP.NET Core 2.2](https://docs.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-2.2?view=aspnetcore-2.2) instead of [Java Spring](https://spring.io/).


## Documentation
[USAGE.md](docs/USAGE.md) explains how to containerize the microservice, debug it, run its unit tests, and contains a quick-start guide for interacting with the microservice once it's running. It also explains how to use OAuth2 scopes to provide course-grained authorization around the microservice's API.

- [Running this microservice locally inside a container](docs/USAGE.md#running-this-microservice-locally-inside-a-container)
- [Debugging using Visual Studio Code](docs/USAGE.md#debugging-using-visual-studio-code)
- [Debugging unit tests using Visual Studio Code](docs/USAGE.md#debugging-unit-tests-using-visual-studio-code)
- [Running from the command line without containerization](docs/USAGE.md#running-from-the-command-line-without-containerization)
- [Readiness and liveness checks](docs/USAGE.md#readiness-and-liveness-checks)
- [Experimenting with API operations](docs/USAGE.md#experimenting-with-api-operations)
- [Writing code to interact with this service](docs/USAGE.md#writing-code-to-interact-with-this-service)
- [Environment variable configuration](docs/USAGE.md#environment-variable-configuration)
- [Quick-start guide](docs/USAGE.md#quick-start-guide)
- [Data pipelining](docs/USAGE.md#data-pipelining)
- [Bulk importing of Json arrays and Csv files](docs/USAGE.md#bulk-importing-of-json-arrays-and-csv-files)
- [Authorization and Security](docs/USAGE.md#authorization-and-security)

## License
The repository utilizes code licensed under the terms of the Apache Software License and therefore is licensed under ASL v2 or later.

This source code in this repository is free: you can redistribute it and/or modify it under the terms of the Apache Software License version 2, or (at your option) any later version.

This source code in this repository is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE. See the Apache Software License for more details.

You should have received a copy of the Apache Software License along with this program. If not, see https://www.apache.org/licenses/LICENSE-2.0.html.

The source code forked from other open source projects will inherit its license.
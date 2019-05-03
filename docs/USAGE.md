# How to use the FDNS Object Service

## Table of Contents
- [Running this microservice locally inside a container](#running-this-microservice-locally-inside-a-container)
- [Debugging using Visual Studio Code](#debugging-using-visual-studio-code)
- [Debugging unit tests using Visual Studio Code](#debugging-unit-tests-using-visual-studio-code)
- [Running from the command line without containerization](#running-from-the-command-line-without-containerization)
- [Readiness and liveness checks](#readiness-and-liveness-checks)
- [Experimenting with API operations](#experimenting-with-api-operations)
- [Writing code to interact with this service](#writing-code-to-interact-with-this-service)
- [Environment variable configuration](#environment-variable-configuration)
- [Quick-start guide](#quick-start-guide)
- [Data pipelining](#data-pipelining)
- [Bulk importing of Json arrays and Csv files](#bulk-importing-of-json-arrays-and-csv-files)
- [Authorization and Security](#authorization-and-security)

## Running this microservice locally inside a container
You will need to have the following software installed to run this microservice:

- [Docker](https://docs.docker.com/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- **Windows Users**: This project uses `Make`. Please use [Cygwin](https://www.cygwin.com/) or the [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10) for running the commands in this README.

1. Open Bash or a Bash-like terminal
1. Build the container image by running `make docker-build`
1. Start the container by running `make docker-start`
1. Open a web browser and point to [http://127.0.0.1:9090/](http://127.0.0.1:9090/)

## Debugging using Visual Studio Code

You will need to have the following software installed to debug this microservice:

- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [.NET Core SDK 2.2](https://www.microsoft.com/net/download)
- [Docker](https://docs.docker.com/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)

1. Open a terminal window
1. `cd` to the `fdns-ms-dotnet-object/src` folder
1. Execute `docker-compose up -d`
1. Open Visual Studio Code
1. Select **File** > **OpenFolder** and select `fdns-ms-dotnet-object/src`
1. Open Visual Studio Code's **Debug** pane (shortcut key: `CTRL`+`SHIFT`+`D`)
1. Press the green arrow at the top of the **Debug** pane
1. Open a web browser and point to https://localhost:5001

## Debugging unit tests using Visual Studio Code

1. Open Visual Studio Code
1. Select **File** > **OpenFolder** and select `fdns-ms-dotnet-object/tests`
1. Open Visual Studio Code's **Explorer** pane (shortcut key: `CTRL`+`SHIFT`+`E`)
1. Open a Test classfile from the file list
1. Select **Debug test** at the top of any of the test methods or **Debug all tests** from the top of the class definition

## Running from the command line without containerization

To run the service from the command line as a regular ASP.NET Core web app:

1. Open a terminal window
1. `cd` to the `fdns-ms-dotnet-object/src` folder
1. Execute `docker-compose up -d`
1. Execute `dotnet restore`
1. Execute `dotnet build`
1. Execute `dotnet run`
1. Open a web browser and point to https://localhost:5001

To run tests from the command line:

1. Open Bash or a Bash-like terminal
1. `cd` to the `fdns-ms-dotnet-object/tests` folder
1. Execute `dotnet test`

## Readiness and liveness checks

A liveness check is available at `/health/live`. The liveness check returns a 200 OK if the service is running.

A readiness check is available at `/health/ready`. The readiness check shows a status of dependent services such as databases and other microservices. It also checks for degraded performance in addition to whether the services are available.

## Experimenting with API operations

We use Swagger to automatically generate a live design document based on the underlying C# source code and XML code comments. Swagger allows developers to experiment with and test the API on a running microservice. It also shows you exactly what operations this service exposes to developers. To access the Swagger documentation, add `/swagger` to the end of the service's URL in your web browser, e.g. `https://localhost:5001/swagger`.

## Writing code to interact with this service

It's strongly recommended to use an SDK to interact with the Object microservice:

- [FDNS .NET Core SDK](https://github.com/erik1066/fdns-dotnet-sdk)
- [FDNS Java SDK](https://github.com/CDCGov/fdns-java-sdk)
- [FDNS JavaScript SDK](https://github.com/CDCGov/fdns-js-sdk)

If an SDK is unavailable for your language or cannot meet a specific need, then interacting with this service can be done by writing standard HTTP calls.

## Environment variable configuration

* `OBJECT_PORT`: A configurable port the application is set to run on
* `OBJECT_MONGO_CONNECTION_STRING`: Standard MongoDB connection string, ex: `mongodb://localhost:27017`
* `OBJECT_MONGO_USE_SSL`: Whether to force TLS 1.2 for the MongoDB connection, ex: `true`
* `OBJECT_FLUENTD_HOST`: The [Fluentd](https://www.fluentd.org/) hostname
* `OBJECT_FLUENTD_PORT`: The [Fluentd](https://www.fluentd.org/) port number
* `OBJECT_PROXY_HOSTNAME`: The hostname of your environment for use with Swagger UI, ex: `api.my.org`
* `OBJECT_IMMUTABLE`: This is a `;` separated list of database/collection names which are immutable collections. Ex: `bookstore/customer;coffeeshop/order`

The following environment variables can be used to configure this microservice to use your OAuth2 provider:

* `OAUTH2_ACCESS_TOKEN_URI`: This is the introspection URL of your provider, ex: `https://hydra:4444/oauth2/introspect`
* `OAUTH2_PROTECTED_URIS`: This is a path for which routes are to be restricted, ex: `/api/1.0/**`
* `OAUTH2_CLIENT_ID`: This is your OAuth 2 client id with the provider
* `OAUTH2_CLIENT_SECRET`: This is your OAuth 2 client secret with the provider
* `SSL_VERIFYING_DISABLE`: This is an option to disable SSL verification, you can disable this when testing locally but this should be set to `false` for all production systems

For more information on using OAuth2 with this microservice, see **Authorization and security** at the end of this document.

## Quick-start guide

Let's try some example CRUD operations. Open the route titled "Inserts an object with a specified ID" on the Swagger page and press the **Try it out** button. Fill in `1` for the object's Id, `bookstore` for the database name, and `customer` for the collection name.

> The database and collection will be created if they don't already exist

Enter the following Json into the request body:

```json
{ "name": "Sarah", "age": 32 }
```

Press **Execute**. You will see an HTTP 201 with the following response body:

```json
{ "_id" : 1, "name" : "Sarah", "age" : 32 }
```

Notice the response headers. They include a URI to the location of the newly-created object:

```
access-control-allow-credentials: true
access-control-allow-origin: *
content-type: application/json; charset=utf-8
date: Wed, 05 Dec 2018 13:54:06 GMT
location: https://localhost:9090/api/1.0/bookstore/customer/1
server: Kestrel
transfer-encoding: chunked
vary: Origin
```

Let's retrieve the object we just inserted. Open the route titled "Gets an object" on the Swagger page and press the **Try it out** button. Fill in `1` for the object's Id, `bookstore` for the database name, `customer` for the collection name, and press **Execute**. We receive the same response body:

```json
{ "_id" : 1, "name" : "Sarah", "age" : 32 }
```

If you change the Id to 2 and press **Execute**, notice you will receive an HTTP 404 "Not Found" response.

Let's update this record and make Sarah a little older. Open the route titled "Updates an object" on the Swagger page and press the **Try it out** button. Fill in `1` for the object's Id, `bookstore` for the database name, and `customer` for the collection name. Enter the following Json into the request body:

```json
{ "name": "Sarah", "age": 42 }
```

Press **Execute**. We receive the updated object:

```json
{ "_id" : 1, "name" : "Sarah", "age" : 42 }
```

> The PUT verb that maps to a database UPDATE operation is a wholesale replacement of the object. Whatever you submit overwrites the current object in the underlying database.

Let's now try to find some records to see how to use the Find route. Before we can do this, insert the following records with `id` values of 2, 3, 4, and 5:

```json
{ "name": "John", "age": 35 }
```
```json
{ "name": "Mary", "age": 65 }
```
```json
{ "name": "Ramona", "age": 75 }
```
```json
{ "name": "Maria", "age": 42 }
```

Open the route titled Finds one or more objects that match the specified criteria" on the Swagger page and press the **Try it out** button. The `findExpression` property is the most important and the most powerful. It allows using [MongoDB-style find query sytnax](https://docs.mongodb.com/manual/reference/method/db.collection.find/), which we strongly recommend referencing to get the most out of the Object service. Let's do a simple find on everyone whose age is 42. (Both Maria and Sarah should have `age` values of 42 if you've followed all of the previous instructions.) Enter the following into the `findExpression` box:

```json
{ age: 42 }
```

Fill in `bookstore` for the database name and `customer` for the collection name. Do not fill in any of the other inputs and press **Execute**. Notice two objects are returned in an array:

```json
[
  {
    "_id": "1",
    "name": "Sarah",
    "age": 42
  },
  {
    "_id": "5",
    "name": "Maria",
    "age": 42
  }
]
```

Let's find out who has an age less than 45. Change the `findExpression` to the following:

```json
{ age: { $lt: 45 } }
```

Press **Execute** and observe the following matching records are returned in a Json array:

```json
[
  {
    "_id": "1",
    "name": "Sarah",
    "age": 42
  },
  {
    "_id": "2",
    "name": "John",
    "age": 35
  },
  {
    "_id": "5",
    "name": "Maria",
    "age": 42
  }
]
```

## Data pipelining

The Object service also supports data pipelines via the [MongoDB aggregation framework](https://docs.mongodb.com/manual/aggregation/).

In short, you can define an ordered set of complex transformation and filtering stages in a pipeline for MongoDB to process. The result of executing the pipeline is then returned to the user via the Object service.

To see how this works, first insert the following records into the `books` collection of the `bookstore` database:

```json
{ "_id" : 1, "title": "The Red Badge of Courage", "author" : "Stephen Crane", "pages": 112, "isbn": { "isbn-10" : "0486264653", "isbn-13" : "978-0486264653" } }
{ "_id" : 2, "title": "Don Quixote", "author" : "Miguel De Cervantes", "pages": 992, "isbn": { "isbn-10" : "0060934344", "isbn-13" : "978-0060934347" } }
{ "_id" : 3, "title": "The Secret Garden", "author" : "Frances Hodgson Burnett", "pages": 126, "isbn": { "isbn-10" : "1514665956", "isbn-13" : "978-1514665954" } }
{ "_id" : 4, "title": "A Connecticut Yankee in King Arthur's Court", "author" : "Mark Twain", "pages": 116, "isbn": { "isbn-10" : "1517061385", "isbn-13" : "978-1517061388" } }
{ "_id" : 5, "title": "Moby Dick; Or The Whale", "author" : "Herman Melville", "pages": 458, "isbn": { "isbn-10" : "161382310X", "isbn-13" : "978-1613823101" } }
{ "_id" : 6, "title": "Faust", "author" : "Johann Wolfgang Von Goethe", "pages": 158, "isbn": { "isbn-10" : "1503262146", "isbn-13" : "978-1503262140" } }
```

### Matching

Next, find the `aggregate` route in the Swagger. Specify `books` for the collection and `bookstore` for the database. The first operation we're going to try is a simple `$match` using a regular expression. The [$match operation](https://docs.mongodb.com/manual/reference/operator/aggregation/match/) acts like a search query. Enter the following Json into the `payload` field:

```json
[
  { $match: { title: /^(the|a)/i } }
]
```

The following results should appear: _The Red Badge of Courage_, _The Secret Garden_, and _A Connecticut Yankee in King Arthur's Court_.

We can add additional conditions to the `$match` operation. For example, if we want to filter the list by all books with a page count of more than 120 pages, we can add another condition:

```json
[
  { $match: { title: /^(the|a)/i, pages: { $gt: 120 } } }
]
```

### Sorting

We can add a sort pipeline to sort the returned books in descending order based on their page count:

```json
[
  { $match: { title: /^(the|a)/i } },
  { $sort: { pages : -1 } }
]
```

### Limiting

If we want to limit our result set to just 2 books, we can do that by adding a `$limit` stage to the pipeline:

```json
[
  { $match: { title: /^(the|a)/i } },
  { $sort: { pages : -1 } },
  { $limit: 2 }
]
```

### Counting

If we want a plain count of the items, rather than all the items, we can do that by adding a `$count` stage:

```json
[
  { $match: { title: /^(the|a)/i } },
  { $sort: { pages : -1 } },
  { $limit: 2 },
  { $count: "numberOfBooks" }
]
```

### Bucketing

We can also [bucket](https://docs.mongodb.com/manual/reference/operator/aggregation/bucket/#pipe._S_bucket) books into categories. In the example below, books are categorized by their page count, with each category listing the titles of all matching books:

```json
[
  {
    $bucket: {
      groupBy: "$pages",
      boundaries: [ 0, 200, 400, 1000 ],
      default: "Invalid",
      output: {
        "count": { $sum: 1 },
        "titles" : { $push: "$title" }
      }
    }
  }     
]
```

### Other pipeline stages

MongoDB supports many more types of pipeline stages. This document has only described some of the most simple ones for the sake of brevity. Please see https://docs.mongodb.com/manual/reference/operator/aggregation-pipeline/ for a comprehensive list of supported pipeline stages. See https://docs.mongodb.com/manual/reference/operator/aggregation/ for a list of operators you can use in conjunction with pipeline stages.

## Bulk importing of Json arrays and Csv files

The service allows bulk importing of objects via Json arrays and Csv files. See the `/multi` and `/csv` routes, respectively. 

For example, on the `/multi` route, one can insert an array of Json:

```json
[
    { "title": "Don Quixote", "author" : " Miguel De Cervantes", "pages": 992 },
    { "title": "The Secret Garden", "author" : "Frances Hodgson Burnett", "pages": 126 },
    { "title": "Moby Dick; Or The Whale", "author" : "Herman Melville", "pages": 458 },
    { "title": "Faust", "author" : "Johann Wolfgang Von Goethe", "pages": 158 }
]
```

The service will return the number of inserted objects and the Ids of those objects:

```json
{
  "inserted": 4,
  "ids": [
    "5c0c1f902ab79c00011bd5ab",
    "5c0c1f902ab79c00011bd5ac",
    "5c0c1f902ab79c00011bd5ad",
    "5c0c1f902ab79c00011bd5ae"
  ]
}
```

## Authorization and Security

This microservice is configurable so that it can be secured via an OAuth2 provider. Each route on the microservice is mapped to a scope. Since the database and collection names are part of the route, and since an OAuth2 token is only valid for the scopes associated with that token, then using OAuth2 and scopes are an effective way to control data access. Consider if a client application called `bookstore` has the following scopes:

```
fdns.object.bookstore.customers.read
fdns.object.bookstore.customers.insert
fdns.object.bookstore.customers.update
fdns.object.bookstore.books.read
fdns.object.bookstore.books.insert
fdns.object.bookstore.books.update
fdns.object.bookstore.orders.read
fdns.object.bookstore.orders.insert
```

The `bookstore` client application can access `GET api/1.0/bookstore/customers/1` because that route is mapped to one of the above scopes. If the `bookstore` client application instead tried to access `GET api/1.0/coffeshop/orders/15`, they would be denied becase that route is not part of the scope associated with `bookstore`'s access token. CRUD operations can also be controlled at this level such that `PUT api/1.0/coffeshop/orders/8` would be denied, as this corresponds to an UPDATE operation and the above scopes do not include UPDATE rights on the `bookstore/orders` route. Each software application that uses Object can (and should!) be given a different set of scopes to ensure no other applications can access their data.

Using scopes in this manner allows the Object microservice to store data for many different applications without presenting authorization risks across those boundaries. Note that OAuth2 also allows a "resource-owner" consent flow for individual users. This mechanism could be used to grant read-only access to a specific data collection for data scientists and analysts, who could then access the data directory via the API in their preferred statistical tool, e.g. SAS.

An OAuth2-based authorization model with per-application scopes that map to routes and HTTP verbs is part of how the Object microservice can be used across the enterprise as part of an enterprise-grade "data lake."

Note that additional Foundation Services provide OAuth2 integration with LDAP and ActiveDirectory.

__Scopes__: This application uses the following scope: `fdns.object.*`
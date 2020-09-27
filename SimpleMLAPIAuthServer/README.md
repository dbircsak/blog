# Simple MLAPI Authoritative Server
This is my try at a simple authoritative server: where the clients send their commands to the server, the server decides where everyone moves to on the map and sends back the location of all clients to everyone. There is no delta compression when serializing, no client-side prediction or server reconciliation, no lag compensation. In other words: very very simple.

Code for [blog post](http://darrellbircsak.com/2020/09/27/simple-mlapi-authoritative-server/)

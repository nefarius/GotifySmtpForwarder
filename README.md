# GotifySmtpForwarder

Forwards e-mails to a Gotify application.

## About

This project spawns an SMTP server and forwards received e-mail messages to a [Gotify](https://gotify.net/) application of your choice. That's it 😄

## Why?

A lot of popular web applications (like WordPress) only support notifying you via SMTP or via plugins you need to install and keep an eye on. This little service can run alongside e.g. a WordPress container and receive messages from it. The messages will then be forwarded to a Gotify server you can host yourself, so no need to have a mail gateway or relay and deal with the complications of them.

## 3rd party credits

- [SmtpServer](https://github.com/cosullivan/SmtpServer)
- [Refit](https://github.com/reactiveui/refit)

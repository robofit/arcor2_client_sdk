# Legacy Models

Some libraries can implement backward-compatibility features (e.g., method uses a series of older RPCs to simulate a new unavailable RPC when communicating with older version servers) that rely on older generated data models.
This directory serves as a container for old and outdated OponAPI data models, so they don't get deleted on regeneration.


## `Communication` Library

Mark compatibility methods for the `Arcor2Client` class and working with these older data models as `[Obsolete("Insert reason and up to which server version is this supported.")]`

## `ClientServices` Library

This library should not directly expose any legacy or compatibility-focused members, but only use the ones provided by the `Communication` library.
The usage should ideally be internal (e.g. not exposed to the user). For, example, if an RPC identificator is renamed, this library should expose the functionality as one method. Internally, it should switch between calling `Arcor2Client.NewRpcMethod` or `Arcor2Client.ObsoleteRpcMethod` based on the determined (or supplied) server version.
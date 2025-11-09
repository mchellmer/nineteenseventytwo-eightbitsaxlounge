# Overview
The midi layer interacts with midi devices sending midi messages to set a desired state of effects. The layer receives and responds to messages via an api. It is deployed to the machine where the midi devices are connected via USB.

# PC Implementation
Use Winmm.dll as midi device integration in a dotnet solution with minimal api.

## Minimal API
- standard web api dotnet template -> ASP.Net minimal api project
- define endpoints
  - authentication
  - dev environment
  - api service
  - midi
    - PUT {deviceName}: sets db and device to provided config
      - db exists ? -> : create
      - relevant document(s) exist(s) ? -> : create
      - create instance of MidiDevice object from document
      - update object with provided config
      - set midi device to provided config
      - success ? update db : throw error
    - PUT {deviceName}/reset: sets db and device to defaults
      - db exists ? -> : create
      - relevant document(s) exist(s) ? -> : create
      - set midi device to default config

## Library
- midi device document model
- db/ventrisdualreverb
  - Name
    Description
    ActiveEngineA: true/false - only one engine active at a time
    ActiveEngineB: true/false - only one engine active at a time
    EngineParameters:
      Id:
      Name:
      Description:
      MidiControlChangeNumberEngineA:
      MidiControlChangeNumberEngineB:
      MidiControlChangeValueEngineA:
      MidiControlChangeValueEngineB:
    MidiControlChangeNumberEngineA:
    MidiControlChangeNumberEngineB:
    MidiControlChangeValueEngineA:
    MidiControlChangeValueEngineB:
    MidiControlChangeValues: optional for parameters with discrete number of options
      Name
      MidiControlChangeValueEngineA:
      MidiControlChangeValueEngineB:
    MidiControlChangeValueMin: optional usually 0
    MidiControlChangeValueMax: optional usually 127
    Size: only for engines that support this
    Sizes: ditto - int number of sizes


- data access service? e.g. in sql stored procedures manage access i.e. save/load operations in a standard way
- implement IMidiDeviceService reset, put
- implement IMidiDataService reset, put

## Tests
- test project

# Development
- new dotnet solution with class library to hold data models
  - couchdb data access via data layer
  - midi device config via winmm.dll
- minimal api project
- integrate with data layer
  - REST methods to manage documents
- methods defined to maintain data in db e.g. seed db with static midi info
- methods defined to manage midi device state e.g. set 'enginea' to 'Hall'
- ci/cd managed release of some sort

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
- devices
  - Name: VentrisDualReverb
    Description: Reverb effects pedal with two engines.
    Active: true
    Effects:
      Name: ReverbEngineA
      Active: true
      MidiControlChangeNumber: 1
      MidiControlChangeValueSelector: reverbengine
      EffectSettings: # dynamic values set by app
        Name: ReverbEngine
        Value: Room

        Name: PreDelay
        Value: 0

        Name: Time
        Value: 0

        Name: Control1
        Value: 0 # 0-127 or selector dependent

        Name: Control2
        Value: 0
      
      Name: ReverbEngineB
      etc
    
- selectors (size,dual/single mode)
  - Name: ReverbEngine
    Selections:
      Name: Room
      MidiCCValue: 0
      etc

- effects
  - Name: Room
    Description: text
    Settings:
      Name: Control1
      Effect: Bass
      DeviceEffect: Bass # Effect with specific midi config, some effects can be e.g. EngineParam1-5

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

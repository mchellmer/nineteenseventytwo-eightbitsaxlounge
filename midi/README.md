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
- midi device data model
  - the data model must store device and effect details to be used by the 8 bit sax lounge app (CRUD implemented via data layer)
    - UI gets effect details and values to display
    - midi app gets/puts device/effect midi details
      - control change message: control change address, control change value
  - dbs
    - devices - details of device properties and state e.g. name/description, is it active, list of effects and states
    - selectors - maps effects having discrete/named midi values e.g. reverb engine A set to Room is CC address 0, value 0; differs from e.g. Bass having a range of 0-127
    - effects - name/description and device specific settings
  - ventris dual reverb example
    - the device model will mirror the physical device
      - knob: set engine (e.g. room, plate, spring)
      - knob: time - length of time to apply reverb effect
      - knob: pre-delay - delay between dry signal and applying reverb
      - knob: control 1 - engine epecific setting e.g. Room 1 Control 1 = Bass
      - knob: control 2 - same
      - knob: EngineA or B or both active
    - the midi implementation defines midi control change message details required to adjust engine similar to knobs
    - some knobs midi implementation depends on the active engine e.g. EchoVerb Control1 is 'PreDelayFeedback' with a static CC address for the device, EchoVerb Control2 is 'DelayReverbCrossfade' and uses midi address for EngineParam5
    - Sample request: update VentrisDualReverb effect ReverbEngineA to EchoVerb
      - PUT VentrisDualReverb.Effects.ReverbEngineA.EffectSettings?Name=ReverbEngine
        - Value=EchoVerb
      - cc message to midi device
        - CC#=GET devices.VentrisDualReverb.MidiImplementation?Name=ReverbEngine.ControlChangeAddress?Name=ReverbEngineA.Value
        - CCValue=GET selectors.ReverbEngine.Selections?Name=Echoverb.MidiControlChangeValue
    - Sample request: update VentrisDualReverb effect ReverbEngineA control2 'knob' to 7
      - PUT VentrisDualReverb.Effects.ReverbEngineA.EffectSettings?Name=Control1
        - Value=7
      - cc message to midi device
        - EffectName = GET devices/VentrisDualReverb.Effects.ReverbEngineA.EffectSettings?Name=ReverbEngine.Value
        - VentrisDualReverbMidiParam = GET effects/(EffectName).DeviceSettings?DeviceName=VentrisDualReverb.Control1.Parameter
        CC#=GET devices.VentrisDualReverb.MidiImplementation?Name=(VentrisDualReverbMidiParam).ControlChangeAddress?Name=ReverbEngineA.Value

    - Sample db documents:
      - devices
        - Name: VentrisDualReverb
          Description: Reverb effects pedal with two engines.
          Active: true
          MidiImplementation:
            Name: ReverbEngine
            ControlChangeValueSelector: ReverbEngine
            ControlChangeAddresses:
              Name: ReverbEngineA
              Value: 1
              Name: ReverbEngineB
              Value: 27
            
            Name: Time
            (ControlChangeValueSelector ? db/selectors.value : value ranges from 0-127)
            etc

            Name: PreDelayFeedback
            etc

            Name: EngineParam5
            etc

          DeviceEffects:
            Name: ReverbEngineA
            Active: true
            DefaultActive: true
            EffectSettings: # dynamic values set by app
              Name: ReverbEngine
              DefaultValue: 0
              Value: 8 # for Echoverb

              Name: PreDelay
              Value: 0
              Name: Time
              Value: 0

              Name: Control1
              DeviceEffectSettingDependencyName: ReverbEngine
              Value: 0 # 0-127 or selector dependent

              Name: Control2
              Value: 0
            
            Name: ReverbEngineB
            etc
          
      - selectors (size,dual/single mode)
        - Name: ReverbEngine
          Selections:
            Name: EchoVerb
            MidiCCValue: 8
            etc

      - effects
        - Name: ReverbEngine ????????????
          Descriptoin: text
          DeviceSettings:
            
        - Name: EchoVerb
          Description: text
          DeviceSettings:
            Name: Control1
            DeviceName: VentrisDualReverb
            EffectName: PreDelayFeedback 
            DeviceMidiImplementationName: EngineParam1-5 or engine agnostic i.e. this.EffectName
            
            DeviceName: VentrisDualReverb
            Name: Control2
            EffectName: DelayReverbCrossfade
            DeviceMidiImplementationName: EngineParameter5
        
        - Name: PreDelayFeedback
          Description: text
        
        - Name: DelayReverbCrossfade
          Description: text

- data access service? e.g. in sql stored procedures manage access i.e. save/load operations in a standard way
  - use couchdb design doc/ view as db operation
  - use CouchDB.Net package
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

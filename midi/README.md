# Overview
The midi layer interacts with midi devices sending midi messages to set a desired state of effects. The layer receives and responds to messages via an api. It is deployed to the machine where the midi devices are connected via USB.

# PC Implementation
Use Winmm.dll as midi device integration in a dotnet solution with minimal api.

# CI/CD
Ansible Access to PC
- Install OpenSSH (elevated powershell session)
  Check enabled: `Get-WindowsCapability -Online | Where-Object Name -like 'OpenSSH.Server*'`
  Enable: `Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0`
  Start: `Start-Service sshd`
  AutoStart: `Set-Service -Name sshd -StartupType 'Automatic'`
  Confirm Running: `Get-Service sshd`
  Confirm Listening: `Get-NetTCPConnection -LocalPort 22 -State Listen`
  Test localhost: `ssh localhost`
  Add rule allowing connection from ci/cd host: 
  ```
    New-NetFirewallRule -DisplayName "OpenSSH Server (Pi only)" 
        -Name "OpenSSH-Server-CICD" `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort 22 `
        -Action Allow `
        -RemoteAddress <CICD IP> `
        -Profile Any `
        -Enabled True`
  ```
  Restart: `Restart-Service sshd`
  Test from pi: `ssh <username>@<PC IP>`

- Ansible access
  Ensure entry in /etc/ansible/hosts for midi group (handled by server layer)
    ```
      [midi]
      midi-host ansible_host=<PC IP> ansible_user=<PC User> ansible_connection=ssh ansible_shell_type=powershell
    ```
  PC uses powershell by default for ssh (elevated powershell session)
    temProperty -Path "HKLM:\SOFTWARE\OpenSSH" -Name DefaultShell -Value "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -PropertyType String -Force`
  Test: `ansible midi-host -m ansible.windows.win_shell -a "Get-Service sshd" --ask-pass`

- Configure PC via Ansible (ssh keys, service directory and dependencies): `make init-midi`
- Release to PC (take artifact and install): `make deploy-midi`

# Scope
- midi layer hosted on separate machine as data layer (currently case)
  - device midi implimentation handled prior to request
  - midi layer recieves information required to interact with midi device
    - send control change message to named device
- midi layer co-hosted with data layer (future case)
  - minimal device specific logic - layer should get most details to perform midi commands via requests to data layer
    - maintain device specific activation logic e.g. turn on device A or turn off effect A on device A
  - handle requests to set some effect for some device to a provided value
    - value validation
      - min/max midi values smooth effects e.g. volume 0-127
      - min/max midi values for selector effects e.g. reverbEngine on device A has 13 options

## Minimal API
- standard web api dotnet template -> ASP.Net minimal api project
- define endpoints
  - authentication
    - POST to retrieve temporary jwt token per authorised client
  - midi
    - POST to send midi control change message
    - PUT {deviceName}/reset: sets db and device to defaults (scaffolded, but outside of current scope)

## Library
- midi device data model (scaffolded, but outside current scope)
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

            Name: Size
            ControlChangeValueSelector: Size

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

- Application level logic (scaffolded, but outside of current scope)
  - Activators - each device has effects that activate in complicated ways, add activator logic per device
  - Validation
    - requests to set effect values should be validated/modifed to conform to device midi implimentation
      - validate: request

## Tests
- endpoint handler unit tests
- data service unit tests
- winmm device service unit tests

# Development
- ci/cd managed release of some sort
  - version text
  - build host access via ansible to PC
  - new action pipeline
    - run dotnet tests
    - package app to dll
    - store in github
    - install service on PC via ansible (makefile)

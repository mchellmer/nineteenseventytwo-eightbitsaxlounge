# Feature roadmap
Midi Layer
- seed db operation to init midi config in db

Bot Layer
- integrate with Twitch channel
- respond to commands
- read/update actual and requested midi device state

State Layer
- unified state to ensure consistent: UI and db match midi and chat states
- default state stored and applied as needed
- enforce db as true state of UI and midi device

Data Layer
- requests for midi details handled with appropriate response

Db layer
- source of true state -> UI and device track

Server layer
- all containers enforced probes for health monitoring
- service mesh for finer tuned monitoring
- shared ansible role for common work among layers

Monitoring layer
- grafana configured to show health of app end to end

CI/CD
- linting and scanning
- shared workflow logic for common tasks
- split versioning between app and infra in layers e.g. no change in image/infra no rebuild and deploy of image/infra

Security
- end to end review
- security scanning and monitoring integrated with pipelines

Cloud replication
- capability to spin-up/down infrastructure outside of midi layer in AWS/Azure/gcp
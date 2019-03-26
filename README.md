<img src="https://upload.wikimedia.org/wikipedia/commons/d/d3/Cloud-Machine-Learning-Engine-Logo.svg"></img>
# biped_robot

> Our project is to develop a physically based bipedal
character that can be created in a simulated 3D environment.
with the help of "Unity Machine Learning Agents".
to move independently.


### Requirements
- Unity version 2018.3.7f1
- ML-Agents version 0.7.0


### Unity
- download Unity Hub <a href="https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe" target="_blank"> here </a>
- follow the instauctions of the installer and install Unity version 2018.3.7f1
### Setup ML-AGENTS
- for unix systems <a href="https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Installation.md" target="_blank"> click here </a>  for an installationguide
- for windows systems <a href="https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Installation-Windows.md" target="_blank"> click here </a>  for an installationguide

### Clone

- Clone this repo to your local machine using `https://github.com/F-Brar/biped_robot`

### How to see what they have learned so far
* Checkout the Inference branch
* Start Unity and load the Project
* Click the playbutton and watch the result

### How to start the learning-process inside the editor
* Checkout the learning branch
* Start unity and load the project
* Start "Anaconda Prompt", navigate to the project dir and use this command to start the learning process
```sh
mlagents-learn config/robot_config.yaml —run-id=NAME OF THE RUN ID —train
```

### How to start the learning-process with a build project
* Checkout the learning branch
* Start unity and load the project
* Build the project (click file -> Build Settings -> Build and choose a location to store the executable project)
* Start "Anaconda Prompt", navigate to the project dir and use this command to start the learning process
```sh
mlagents-learn config/robot_config.yaml --env="INSERT PATH TO THE UNITY ENVIRONMENT.EXE" --train --run-id=INSERT NAME OF THE RUN ID
```

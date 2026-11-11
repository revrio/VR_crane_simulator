# Work study and ergonomics research demo

**Current Operational Port:** open `Assets/Scenes/OperationalPort.unity`. Choose a layout (1–4), then Start operation (P in desktop mode). Five containers begin in the yard, with fixed background road traffic, workers and neighbouring cranes. A/D moves the gantry, W/S moves the trolley, arrows hoist and Space locks/releases. Impacts produce feedback and records without freezing the equipment; there is no protected stop, acknowledgement or recovery workflow. Pause and Reset remain available. On Quest 2, left stick controls gantry/trolley, right stick controls hoist, right A locks/releases and right grip enables precision. See [the current setup and validation notes](docs/QUEST_2_READY.md).

The browser build downloads and verifies all runtime assets before enabling **Start simulator**. It uses desktop keyboard/mouse controls and offers session CSV downloads as a ZIP. See [the web build and deployment guide](docs/WEB_DEPLOYMENT.md).

The five-container iteration-2 scene/player is preserved as `Assets/Scenes/OperationalPortIteration2.unity` and `tools/run_desktop_demo.sh --iteration2`. Its [historical guide](docs/OPERATIONAL_ITERATION_2.md) describes the four handling presets.

The first one-container operational chain is preserved as `Assets/Scenes/OperationalPortIteration1.unity`; `tools/run_desktop_demo.sh --iteration1` launches its previous Linux player.

The previous v6 scene and player are available below; `tools/run_desktop_demo.sh --research` launches that player explicitly.

Open **Assets/Scenes/SeaportResearch.unity** in **Unity 6000.0.26f1**, then press Play. Click **Practice** and Maya, the port instructor NPC, guides each action with offline speech and captions. Press **C** for the seated cabin with Maya, joystick consoles and a live load-camera monitor. Tab cycles all four views; Shift slows motion, and on-screen buttons also operate the crane. Choose A / spread bays, B / compact bays, or C / stacked pickup. The scene supports keyboard/mouse testing and tracked VR controllers, explicit practice/measured runs, four work elements, placement validation, and checkpointed CSV export.

The cabin keeps one live load monitor and a clear forward view; guidance and run controls stay in the right console. A visible overhead boom/trolley supports four steel hoist cables and a detailed twistlock spreader. Corrugated task containers have door hardware, corner fittings and ID markings. Forty extra freight containers, animated harbor water, drifting clouds and a distant tugboat fill out the port. Layout C unloads C1 from the top, then C2, then C3 into separate bays.

Read [the demo guide](docs/DEMO_GUIDE.md) for controls, the Quest Link setup, exported data, and verification. The [project review and experiment plan](docs/WSD_ERGONOMICS_PLAN.md) explains the time study, method comparison, normal/standard-time assumptions, and final-study protocol.

The original simulator is credited below. The research scene and instrumentation adapt it for a course project. Desktop verification does not establish headset ergonomics or industrial crane fidelity.

---

# VR Crane Simulator: Occupational Health & Safety Training

A physics-driven Virtual Reality prototype designed to provide foundational crane operation experience, focusing on Occupational Health and Safety (OHS). Developed during my tenure as an Application and Simulation Developer Intern, this project prioritizes core mechanics and rapid prototyping to demonstrate practical safety concepts.

## 📋 Project Overview

The simulator places users in a risk-free virtual industrial environment, allowing them to practice basic load management and spatial awareness. To ensure a rapid development cycle and focus primarily on VR interactions, the project utilizes pre-made 3D visual assets. Therefore, rather than focusing on high-fidelity visual realism, the "realistic" aspect of this simulation is heavily dependent on utilizing the physics engine to test hazard prevention and basic mechanical handling.

🎮 **Click here to visit the Game Page!:** https://sidequestvr.com/app/43243/vr-tower-crane-simulator

## ✨ Key Features

- **Physics-Based Mechanics:** While not visually photorealistic, the simulation leverages the physics engine to simulate cable tension, load sway, and mechanical momentum to replicate operational challenges.
- **Rapid Prototyping Approach:** Built using ready-made visual assets, allowing the development focus to remain strictly on C# scripting, VR interactions, and the evaluation engine.
- **Dynamic Scoring & Evaluation Engine:** A real-time assessment system that grades performance based on:
  - **Safety Penalties:** Deducts points for critical errors, load collisions, sudden movements, or violations of basic OHS protocols.
  - **Operational Efficiency:** Rewards smooth handling and optimal completion times.
- **Universal VR Compatibility:** Primarily developed for **Oculus (Meta Quest)** using **Unity 6**, yet compatible with major VR platforms via the XR Interaction Toolkit.

## 🛠 Technical Stack

- **Game Engine:** Unity 6 (LTS)
- **Target Platform:** Meta Quest (Oculus) / Cross-Platform VR
- **XR Framework:** Unity XR Interaction Toolkit / OpenXR
- **Programming Language:** C#

## 🚀 Getting Started

### Prerequisites
- Unity 6 or higher.
- A compatible VR Headset (Meta Quest 2/3/Pro recommended).
- Oculus Link or Air Link for PC-based testing.

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/aliyunuscan/VRCraneSimulator.git
   ```
2. Open the project in the **Unity Hub** using **Unity 6**.
3. Ensure **XR Plug-in Management** is configured for your device in Project Settings.
4. Open the main scene located in `Assets/Scenes/MainSimulation`.
5. Build and run on your headset or use the Unity Editor's Play mode with a linked device.

## 🛡️ OHS Training Focus

This project transcends basic gameplay by acting as a professional training tool. It strictly enforces industrial safety guidelines, testing the operator's reaction times to simulated workplace hazards and their adherence to strict operational limits.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

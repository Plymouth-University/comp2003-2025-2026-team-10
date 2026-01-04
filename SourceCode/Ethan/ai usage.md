# AI Usage Log – COMP2003 CFD Optimization Project

## Project: OpenFOAM Wind Turbine Visualization Optimization  


---

## Overview

This log documents the **intentional, critical, and transparent use of AI tools** throughout the COMP2003 project. AI was used strictly as a **support tool** for research, reasoning, structuring, and limited code scaffolding. All outputs were **validated, modified, and contextualised** using computing fundamentals, empirical testing, and academic judgement.

Primary AI tools used:
- **GPT-5** – technical reasoning, algorithm exploration, code scaffolding, debugging support  
- **Claude** – long-form reasoning, documentation structure, synthesis of complex technical material  

For all research-oriented prompts, AI was explicitly instructed to:
- Cite sources or standards where applicable  
- Avoid speculation or unverifiable claims  
- Stay within the defined project scope  
Any AI output lacking verifiable grounding was either corrected or discarded.

---

## Sprint 1 – Problem Definition & Scope Refinement

### Entry 1
- **Date:** 2025-10-07  
- **Sprint:** 1  
- **Task:** Understanding why existing CFD visualisation pipelines fail at real-time performance  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “Explain why tools like ParaView struggle with real-time visualisation of large CFD datasets. Provide technical reasons and references.”  
- **AI Output:** Explanation of offline rendering pipelines, memory pressure, I/O bottlenecks, and lack of real-time optimisation.  
- **Modifications Made:** Removed generic statements; aligned explanations directly to Unity vs ParaView architectural differences.  
- **Validation Method:** Comparison with ParaView documentation and observed performance of existing project builds.  
- **Computing Fundamentals Applied:** Memory hierarchy, data throughput, rendering pipelines.  
- **Justification:** Accelerated initial understanding of the problem space before deeper manual analysis.  
- **Time Saved:** ~2 hours  

---

### Entry 2
- **Date:** 2025-10-10  
- **Sprint:** 1  
- **Task:** Clarifying acceptable abstraction levels for visualisation vs simulation  
- **AI Tool:** Claude  
- **Context/Prompt:** “Explain the difference between numerical accuracy and perceptual realism in real-time graphics, with academic references.”  
- **AI Output:** Discussion of abstraction, perceptual thresholds, and analytical models used in real-time systems.  
- **Modifications Made:** Explicitly constrained claims to *qualitative visualisation*, not engineering validation.  
- **Validation Method:** Cross-checked against graphics literature and project brief constraints.  
- **Computing Fundamentals Applied:** Abstraction, approximation, modelling trade-offs.  
- **Justification:** Needed defensible academic language for scope justification.  
- **Time Saved:** ~1 hour  

---

## Sprint 2 – Research into Optimisation Methods

### Entry 3
- **Date:** 2025-10-15  
- **Sprint:** 2  
- **Task:** Investigating temporal interpolation for CFD timestep reduction  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “What is temporal interpolation and how can it reduce time-series simulation data? Keep it generic and non-animation specific.”  
- **AI Output:** Explanation of keyframes, interpolation functions, and sampling trade-offs.  
- **Modifications Made:** Removed animation-centric framing and re-mapped concepts to CFD timesteps.  
- **Validation Method:** Manual reasoning and small-scale timestep reduction tests.  
- **Computing Fundamentals Applied:** Sampling theory, interpolation, time-series reconstruction.  
- **Justification:** Conceptual grounding before implementation by another team member.  
- **Time Saved:** ~1.5 hours  

---

### Entry 4
- **Date:** 2025-10-18  
- **Sprint:** 2  
- **Task:** Understanding Gerstner wave mathematics for procedural ocean modelling  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “Derive the Gerstner wave equation and explain each parameter. Cite graphics references.”  
- **AI Output:** Mathematical formulation and parameter definitions.  
- **Modifications Made:** Corrected parameter constraints and removed assumptions not valid for this project.  
- **Validation Method:** Comparison with graphics textbooks and existing shader implementations.  
- **Computing Fundamentals Applied:** Trigonometry, parametric surfaces, vector math.  
- **Justification:** Faster recall of established equations than manual derivation.  
- **Time Saved:** ~2 hours  

---

## Sprint 3 – Pipeline Architecture Design

### Entry 5
- **Date:** 2025-10-23  
- **Sprint:** 3  
- **Task:** Designing the CFD → ParaView → Python → Unity optimisation pipeline  
- **AI Tool:** Claude  
- **Context/Prompt:** “Propose a modular data pipeline for reducing CFD datasets for real-time rendering. Focus on separation of concerns.”  
- **AI Output:** High-level staged architecture.  
- **Modifications Made:** Removed unrealistic automation and simplified to feasible academic scope.  
- **Validation Method:** Architectural walkthrough and feasibility analysis.  
- **Computing Fundamentals Applied:** Modular design, separation of concerns, data flow control.  
- **Justification:** Structured brainstorming for system design.  
- **Time Saved:** ~1 hour  

---

## Sprint 4 – Procedural Wave MVP

### Entry 6
- **Date:** 2025-10-29  
- **Sprint:** 4  
- **Task:** Creating a procedural Gerstner wave test script  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “Write a Python script to generate Gerstner wave parameters and export them as JSON for Unity.”  
- **AI Output:** Basic parameter generation and JSON export code.  
- **Modifications Made:** Refactored code, validated units, added extensive comments, and enforced stability constraints.  
- **Validation Method:** Visual plotting and Unity integration tests.  
- **Computing Fundamentals Applied:** Coordinate systems, numerical stability, data serialization.  
- **Justification:** Reduced boilerplate coding time.  
- **Time Saved:** ~2 hours  

---

### Entry 7
- **Date:** 2025-10-31  
- **Sprint:** 4  
- **Task:** Debugging unstable wave behaviour  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “Why do Gerstner waves break at high steepness values?”  
- **AI Output:** Explanation of steepness limits.  
- **Modifications Made:** Derived explicit bounds and enforced them programmatically.  
- **Validation Method:** Stress-testing with extreme parameter values.  
- **Computing Fundamentals Applied:** Stability analysis, constraint enforcement.  
- **Justification:** Used AI to identify known theoretical limits.  
- **Time Saved:** ~1 hour  

---

## Sprint 5 – CFD Data Extraction & Analysis

### Entry 8
- **Date:** 2025-11-05  
- **Sprint:** 5  
- **Task:** Understanding `alpha.water` and free-surface extraction  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “Explain the meaning of alpha.water in OpenFOAM. Cite official documentation.”  
- **AI Output:** Description of volume fraction fields in multiphase simulations.  
- **Modifications Made:** Confirmed terminology and excluded solver-specific details not relevant to visualisation.  
- **Validation Method:** OpenFOAM documentation and ParaView inspection.  
- **Computing Fundamentals Applied:** Discretised fields, multiphase modelling.  
- **Justification:** Efficient domain clarification.  
- **Time Saved:** ~45 minutes  

---

### Entry 9
- **Date:** 2025-11-07  
- **Sprint:** 5  
- **Task:** Designing ParaView sampling workflow  
- **AI Tool:** Claude  
- **Context/Prompt:** “How to extract time-series data from ParaView without misinterpreting spatial samples?”  
- **AI Output:** Suggested contour + plot-over-line workflow.  
- **Modifications Made:** Added manual grouping and median extraction per timestep.  
- **Validation Method:** CSV inspection and sanity checks.  
- **Computing Fundamentals Applied:** Data aggregation, statistical robustness.  
- **Justification:** Tool-learning acceleration.  
- **Time Saved:** ~1 hour  

---

## Sprint 6 – Parameter Extraction & Signal Processing

### Entry 10
- **Date:** 2025-11-12  
- **Sprint:** 6  
- **Task:** Extracting wave frequency from CFD-derived surface motion  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “Example FFT-based dominant frequency extraction in Python, with cautions.”  
- **AI Output:** FFT usage example.  
- **Modifications Made:** Added windowing, normalisation, and noise filtering.  
- **Validation Method:** Synthetic signal tests and visual inspection.  
- **Computing Fundamentals Applied:** Signal processing, spectral analysis.  
- **Justification:** Syntax and workflow recall.  
- **Time Saved:** ~1 hour  

---

## Sprint 7 – Evaluation & Performance

### Entry 11
- **Date:** 2025-11-22  
- **Sprint:** 7  
- **Task:** Evaluating performance impact of procedural vs raw data playback  
- **AI Tool:** GPT-5  
- **Context/Prompt:** “How to design a fair performance comparison between two rendering approaches?”  
- **AI Output:** Suggested metrics and controls.  
- **Modifications Made:** Defined fixed camera paths and identical test conditions.  
- **Validation Method:** Unity Profiler measurements.  
- **Computing Fundamentals Applied:** Benchmarking, experimental control.  
- **Justification:** Structured evaluation design.  
- **Time Saved:** ~45 minutes  

---

## Sprint 8 – Documentation & Critical Reflection

### Entry 12
- **Date:** 2025-12-05  
- **Sprint:** 8  
- **Task:** Structuring project plan, design documentation, and AI usage evidence  
- **AI Tool:** Claude  
- **Context/Prompt:** “Structure an academic project plan and AI usage log aligned with UK CS coursework expectations.”  
- **AI Output:** Structured section outlines and guidance.  
- **Modifications Made:** Rewrote content fully, removed generic phrasing, ensured scope accuracy.  
- **Validation Method:** Cross-check against COMP2003 guidance and interim threshold criteria.  
- **Computing Fundamentals Applied:** Software engineering process awareness, professional documentation.  
- **Justification:** Improved clarity and compliance.  
- **Time Saved:** ~2 hours  

---

## Summary Statement

AI tools were used **selectively and critically** to support research, reasoning, documentation, and limited code scaffolding. All AI-generated content was treated as *tentative*, rigorously validated, and refined using computing fundamentals, empirical testing, and academic judgement. AI did not replace implementation, decision-making, or evaluation at any stage of the project.


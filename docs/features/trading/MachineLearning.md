# Machine Learning & Prediction

The `PredictionEngine` provides distinct real-time forecasts for market direction and magnitude using an online learning approach. It does not use pre-trained weights; it learns from the user's running application state.

## 1. Feature Extraction
**Source**: `Strategy/FeatureExtractor.cs`

The model ingests a dictionary of rolling features derived from the last `N` candles.

| Feature Key | Description | Formula |
|-------------|-------------|---------|
| `ret_1` | 1-Period Return | `(Close - PrevClose) / PrevClose` |
| `sma_short` | Short SMA | SMA-5 |
| `sma_long` | Long SMA | SMA-20 |
| `sma_gap` | SMA Divergence | `(SMA5 - SMA20) / SMA20` |
| `rsi` | RSI Momentum | RSI-14 (0-100) |
| `vwap_gap` | VWAP Distance | `(Close - VWAP) / VWAP` (VWAP using HLC3) |

*Note: Insufficient data results in an `error` key being set to -1.*

## 2. Prediction Engine
**Source**: `Strategy/PredictionEngine.cs`

The engine manages three internal models updated via Stochastic Gradient Descent (SGD).

### Models
1.  **Directionality (Logistic Regression)**
    - **Output**: Probability of "Up" (`ScoreUpProbability`).
    - **Decision**:
        - `> 0.55`: MarketDirection.Up
        - `< 0.45`: MarketDirection.Down
        - `Else`: MarketDirection.Flat
2.  **Return Magnitude (Linear Regression)**
    - **Output**: Expected return value (signed).
3.  **Volatility (Linear Regression)**
    - **Output**: Expected absolute move (`Math.Abs(return)`).

### Hyperparameters
- **Learning Rate (LR)**: 0.05
- **Regularization (L2)**: 0.0001
- **Calibration Window**: Rolling 500 samples.

## 3. Online Learning
The `Learn()` method allows the system to update weights based on realized outcomes.

1.  **Update Step**:
    - Calculates the gradient (`Predict - Actual`).
    - Updates bias and weights for all features.
    - Applies L2 regularization decay.
2.  **Calibration Tracking**:
    - **Brier Score**: Measures probabilistic accuracy of the directional model.
    - **MSE**: Measures Mean Squared Error of the return model.

## 4. Reliability Weighting
The system calculates a `ReliabilityWeight` scalar [0-1] to de-risk trades when the model is performing poorly.

```csharp
Weight = 1.0 / (1.0 + 10.0 * CurrentBrierScore)
```

- **Low Error (Brier near 0)** -> Weight near 1.0.
- **High Error (Brier > 0.1)** -> Weight drops rapidly.

#include "UtilityHelper.h"
#include <algorithm>
#include <cmath>

namespace UtilityHelper
{
	double Clamp01(double value)
	{
		return std::clamp(value, 0.0, 1.0);
	}

	double Sigmoid(double value)
	{
		return 1.0 / (1.0 + std::exp(-value));
	}

	double Tanh01(double value)
	{
		return (std::tanh(value) + 1.0) * 0.5;
	}

	double LinearClamp(double value, double minValue, double maxValue)
	{
		if (maxValue < minValue)
			std::swap(minValue, maxValue);

		return std::clamp(value, minValue, maxValue);
	}

	double WeightedSum(const std::vector<double>& inputs, const std::vector<double>& weights, double bias)
	{
		if (inputs.size() != weights.size())
			return bias;

		double sum = bias;
		for (size_t i = 0; i < inputs.size(); ++i)
			sum += inputs[i] * weights[i];

		return sum;
	}

	double EvaluatePerceptron(const std::vector<double>& inputs, const std::vector<double>& weights, double bias, UtilityActivation activation)
	{
		const double raw = WeightedSum(inputs, weights, bias);

		switch (activation)
		{
		case UtilityActivation::Sigmoid:
			return Sigmoid(raw);
		case UtilityActivation::Tanh01:
			return Tanh01(raw);
		case UtilityActivation::LinearClamp:
			return LinearClamp(raw);
		default:
			return Sigmoid(raw);
		}
	}
}

#pragma once
#include <vector>

enum class UtilityActivation
{
	Sigmoid,
	Tanh01,
	LinearClamp
};

namespace UtilityHelper
{
	double Clamp01(double value);
	double Sigmoid(double value);
	double Tanh01(double value);
	double LinearClamp(double value, double minValue = 0.0, double maxValue = 1.0);
	double WeightedSum(const std::vector<double>& inputs, const std::vector<double>& weights, double bias = 0.0);
	double EvaluatePerceptron(const std::vector<double>& inputs, const std::vector<double>& weights, double bias, UtilityActivation activation = UtilityActivation::Sigmoid);
}

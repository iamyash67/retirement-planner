export interface Goal {
  id: number;
  name: string;
  currentAge: number;
  retirementAge: number;
  targetSavings: number;
  monthlyContribution: number;
  currentSavings: number;
}

export interface CreateGoalRequest {
  currentAge: number;
  retirementAge: number;
  targetSavings: number;
  currentSavings: number;
}

export interface AddContributionRequest {
  year: number;
  month: number;
  amount: number;
}

export interface Contribution {
  id: number;
  goalId: number;
  year: number;
  month: number;
  amount: number;
  recordedAt: string;
}

export interface GoalProgress {
  goalId: number;
  progress: string;
}

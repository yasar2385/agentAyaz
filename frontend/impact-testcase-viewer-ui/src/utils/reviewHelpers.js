export function blankReviewDetail(moduleId) {
  return {
    isNew: true,
    masterTestId: '',
    moduleId: moduleId ?? '',
    remarks: [1, 2, 3, 4].map(roundNumber => ({ roundNumber, qaRemark: '', devRemark: '' })),
    testingTypeIds: [],
    clientIds: [],
    masterUpdatedAt: new Date().toISOString(),
  }
}

export function toReviewForm(detail) {
  return {
    masterTestId: detail.masterTestId ?? '',
    masterTestNo: detail.masterTestNo ?? '',
    moduleId: detail.moduleId ?? '',
    preconditionRoleId: detail.preconditionRoleId ?? '',
    masterTypeId: detail.masterTypeId ?? '',
    issueTypeId: detail.issueTypeId ?? '',
    qaStatusId: detail.qaStatusId ?? '',
    devStatusId: detail.devStatusId ?? '',
    masterDescription: detail.masterDescription ?? '',
    masterTestSteps: detail.masterTestSteps ?? '',
    masterTestData: detail.masterTestData ?? '',
    masterExpectedResult: detail.masterExpectedResult ?? '',
    masterActualResult: detail.masterActualResult ?? '',
    testingTypeIds: detail.testingTypeIds ?? [],
    clientIds: detail.clientIds ?? [],
    remarks: detail.remarks ?? [1, 2, 3, 4].map(roundNumber => ({ roundNumber, qaRemark: '', devRemark: '' })),
  }
}

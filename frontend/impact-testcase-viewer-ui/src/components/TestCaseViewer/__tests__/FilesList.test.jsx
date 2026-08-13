import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import FilesList from '../FilesList'

jest.mock('../../../services/testCaseViewerApi', () => ({
  getFiles: jest.fn()
}))

import { getFiles } from '../../../services/testCaseViewerApi'

describe('FilesList', () => {
  beforeEach(() => jest.resetAllMocks())

  it('renders list of files', async () => {
    getFiles.mockResolvedValue([
      { id: '1', name: 'File A', modifiedTime: '2024-01-01T00:00:00Z' }
    ])

    render(<FilesList />)

    await waitFor(() => expect(screen.getByText('File A')).toBeInTheDocument())
  })

  it('shows error on API failure', async () => {
    getFiles.mockRejectedValue(new Error('Network error'))

    render(<FilesList />)

    await waitFor(() => expect(screen.getByText(/Error:/)).toBeInTheDocument())
  })
})

import * as React from 'react';
import videoInfoService from '../services/VideoInfoService';
import { useEffect, useState } from 'react';
import { RequestedAccess } from '../types/ApiModels/RequestedAccessesResponse';
import { format } from 'date-fns'
import { pl } from 'date-fns/locale';
import { ApproveAndRejectButtons } from '../components/ApproveAndRejectsButtons';

export function AdministrateAccessRequestsScreen() {

  useEffect(() => {
    videoInfoService.GetAccessRequests()
      .then(res => {
        setRequestedAccesses(res.accessRequests)
      }).catch(res => {
        console.error(res)
      })
  }, [])


  const formatDate = (date: Date | undefined | null) => {
    if (!date)
      return ""

    // don't ask me
    // todo: find out why I need to cast it to string
    const d = Date.parse(date.toLocaleString())
    return format(d, 'dd MMMM yyyy', { locale: pl })
  }


  const [requestedAccesses, setRequestedAccesses] = useState<Array<RequestedAccess>>([])

  const approveClick = (arg0: React.MouseEvent<HTMLButtonElement, MouseEvent>, request: RequestedAccess, approved: boolean) => {
    const button = arg0.currentTarget;
    button.disabled = true

    if (approved) {
      videoInfoService.ApproveAccessRequest(request)
        .catch(r => {
          console.error(r)
        })
    } else {
      videoInfoService.RejectAccessRequest(request)
        .catch(r => {
          console.error(r)
        })
    }
  }

  const tableContent = () => requestedAccesses.map(el => {
    return <tr key={el.requestId}>
      <td>{el.name}</td>
      <td>{el.requestorFirstName}</td>
      <td>{el.requestorLastName}</td>
      <td>{formatDate(el.whenJoined)}</td>
      <td><ApproveAndRejectButtons
        input={el}
        onApprove={(arg, input) => approveClick(arg, input, true)}
        onReject={(arg, input) => approveClick(arg, input, false)}
      /></td>
    </tr>
  })

  return (
    <div className='container is-max-desktop'>
      <table className="table">
        <thead>
          <tr>
            <th>Nazwa</th>
            <th>Imie</th>
            <th>Nazwisko</th>
            <th>Od kiedy</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {tableContent()}
        </tbody>
      </table>
    </div>
  );
}

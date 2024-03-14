import * as React from 'react';
import videoInfoService from '../services/VideoInfoService';
import { useEffect, useState } from 'react';
import { RequestedAccess } from '../types/ApiModels/RequestedAccessesResponse';

export function AdministrateAccessRequestsScreen() {

  useEffect(() => {
    return () => {
      videoInfoService.GetAccessRequests()
        .then(res => {
          setRequestedAccesses(res.accessRequests)
        }).catch(res => {
          console.error(res)
        })
    };
  }, [])

  const [requestedAccesses, setRequestedAccesses] = useState<Array<RequestedAccess>>([])

  const tableContent = () => requestedAccesses.map(el => {
    return <tr key={el.requestId}>
      <td>{el.name}</td>
      <td>{el.requestorFirstName}</td>
      <td>{el.requestorLastName}</td>
      <td>{el.whenJoined}</td>
    </tr>
  })

  return (
    <div>
      <table className="table">
        <thead>
          <tr>
            <th>Nazwa</th>
            <th>Imie</th>
            <th>Nazwisko</th>
            <th>Od kiedy</th>
          </tr>
        </thead>
        <tbody>
          {tableContent()}
        </tbody>
      </table>
    </div>
  );
}

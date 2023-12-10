import { faCheck, faSpinner } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as React from 'react';
import { useState } from 'react';
import videoInfoService from '../../services/VideoInfoService';
import { Button } from '../Button';
import { Dropdown } from '../Dropdown';
import { IItemToSelect, SelectableList } from './SelectableList';
import { IAssignedEvent, IAssignedGroup } from '../../types/AssignedEventAndGroup';
interface IRequestState {
    wasSend: boolean
    areWeWaiting: boolean
    wasOk: boolean
}

const requestStatusReducer: React.Reducer<IRequestState, Action> = (state: IRequestState, action: Action) => {
    if (action === 'sending')
        return { ...state, areWeWaiting: true, wasSend: true }
    if (action === 'receivedFailed')
        return { ...state, areWeWaiting: false, wasOk: false }
    if (action === 'receivedOk')
        return { ...state, areWeWaiting: false, wasOk: true }

    // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
    throw new Error(`Argument out of range. Action: ${action}`)
}

type Action =
    | "sending"
    | "receivedOk"
    | "receivedFailed"

export function AccessToVideoRequestForm() {

    const [events, setEvents] = useState<Array<IItemToSelect<string>>>([])

    const [availableGroupNames, setAvailableGroupNames] = useState<Array<string>>([])

    const [isSendButtonEnabled, setIsSendButtonEnabled] = useState(false)
    const [selectedGroup, setSelectedGroup] = useState<IAssignedGroup>()

    const [allSharingScopes, setAllSharingScopes] = useState<{ events: Array<IAssignedEvent>, groups: Array<IAssignedGroup> }>({
        events: [],
        groups: []
    })

    const [selectedScopes] = useState<Map<string, boolean>>(new Map<string, boolean>())

    const mapToItemToSelect = (item: IAssignedEvent) => {
        const itemToSelect: IItemToSelect<string> = {
            key: item.id,
            text: item.name,
            selectionDisabled: item.isAssigned
        }

        return itemToSelect
    }

    React.useEffect(() => {
        videoInfoService.GetAvailableEventsAndGroups()
            .then(userGroupAndEvents => {
                const events = new Array<IAssignedEvent>()
                const groups = new Array<IAssignedGroup>()
        
                for(const el of userGroupAndEvents.assigned.events){
                    events.push({...el, isAssigned: true})
                }
        
                for(const el of userGroupAndEvents.available.events){
                    events.push({...el, isAssigned: false})
                }
        
                for(const el of userGroupAndEvents.assigned.groups){
                    groups.push({...el, isAssigned: true})
                }
        
                for(const el of userGroupAndEvents.available.groups){
                    groups.push({...el, isAssigned: false})
                }
                
                setAllSharingScopes({
                    events: events,
                    groups: groups
                })

                const eventsToSet = new Array<IItemToSelect<string>>()

                for (const el of events) {
                    const mapped = mapToItemToSelect(el)
                    eventsToSet.push(mapped)
                }

                setEvents(eventsToSet)

                setAvailableGroupNames(groups.map(r => r.name))
            })
            .catch(e => console.error(e))
    }, [])

    const onGroupSelected = (selectedItem: string, selectedIndex: number) => {
        if (selectedIndex >= 0) {
            setIsSendButtonEnabled(true)
            setSelectedGroup(allSharingScopes.groups[selectedIndex])
        }
    }

    const eventSelected = (item: IItemToSelect<string>, isSelected: boolean) => {
        selectedScopes.set(item.key, isSelected)

        if (isSelected)
            setIsSendButtonEnabled(true)
        else {

            if (selectedGroup)
                return

            let anySelected = false

            selectedScopes.forEach((isSelected, eventKey) => {
                if (isSelected == true) { anySelected = true }
            })

            if (!anySelected)
                setIsSendButtonEnabled(false)
        }
    }

    const [requestStatus, requestStatusDispatch] = React.useReducer(requestStatusReducer, {
        areWeWaiting: false,
        wasOk: false,
        wasSend: false
    })

    const buttonIcon = () => {

        if (!requestStatus.wasSend)
            return "Wyślij"

        if (requestStatus.areWeWaiting)
            return <FontAwesomeIcon className="fa-pulse" icon={faSpinner} />

        if (requestStatus.wasSend) {
            if (requestStatus.wasOk)
                return <span className="icon-text has-text-info">
                    <span className="icon">
                        <FontAwesomeIcon icon={faCheck} />
                    </span>
                    <span>Wysłano</span>
                </span>
            else
                return <span className="icon-text has-text-danger">
                    <span className="icon">
                        <FontAwesomeIcon icon={faCheck} />
                    </span>
                    <span>Coś poszło nie tak :(</span>
                </span>
        }

        throw new Error("Out of range exception. " + JSON.stringify(requestStatus))
    }



    const sendRequest = () => {

        requestStatusDispatch('sending')

        const events = new Array<string>()

        selectedScopes.forEach((isSelected, eventId) => {
            if (isSelected)
                events.push(eventId)
        })

        let groups: Array<string> | undefined = undefined
        if (selectedGroup)
            groups = [selectedGroup?.id]

        const promise = videoInfoService.SendAssigmentRequest(events, groups)
            .then(e => {
                requestStatusDispatch('receivedOk')
            })
            .catch((e) => {
                requestStatusDispatch('receivedFailed')
                console.log(e)
            })
    }

    return (
        <div>
            <div className="content">
                <h2>Wybierz wydarzenia w których brałeś/aś udział i chcesz uzyskać dostęp.</h2>
            </div>
            <div className='columns'>
                <div className='column is-centered has-text-centered'>
                    <div className="box">
                        <h3 className="title">Grupa</h3>
                        <Dropdown isLoading={false}
                            items={availableGroupNames}
                            unselectedText={"Wybierz grupę"}
                            selectedItemIndex={0}
                            onSelected={onGroupSelected}
                            classNames={'is-large'}
                            startWithUnselected={true} />
                    </div>
                </div>
            </div>
            <div className="columns">
                <div className="column">
                    <SelectableList<string>
                        articleClassName='is-info'
                        header='Wydarzenia'
                        onItemStatusChange={eventSelected}
                        text='Wybierz wydarzenia w których brałeś udział!'
                        options={events} />
                </div>
            </div>

            <div className='columns is-mobile is-centered has-text-centered'>
                <div className='column is-centered'>
                    <Button disabled={!isSendButtonEnabled} classNames='is-large' onClick={() => sendRequest()}>
                        {buttonIcon()}
                    </Button>
                </div>
            </div>
        </div>
    );
}

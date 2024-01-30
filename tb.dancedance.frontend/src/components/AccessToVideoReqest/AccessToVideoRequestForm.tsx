import { faCheck, faSpinner } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as React from 'react';
import { useState } from 'react';
import videoInfoService from '../../services/VideoInfoService';
import { Button } from '../Button';
import { Dropdown } from '../Dropdown';
import { IItemToSelect, SelectableList } from './SelectableList';
import { Event, Group } from '../../types/ApiModels/EventsAndGroups';
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
    const [selectedGroup, setSelectedGroup] = useState<Group>()

    const [alreadyAssignedEvents, setAlreadyAssignedEvents] = useState<Array<Event>>([])
    const [alreadyAssignedGroups, setAlreadyAssignedGroups] = useState<Array<Group>>([])

    const [notificationMessage, setNotificationMessage] = useState<string>('')

    const [allSharingScopes, setAllSharingScopes] = useState<{ events: Array<Event>, groups: Array<Group> }>({
        events: [],
        groups: []
    })

    const [selectedScopes] = useState<Map<string, boolean>>(new Map<string, boolean>())

    const mapToItemToSelect = (item: Event) => {
        const itemToSelect: IItemToSelect<string> = {
            key: item.id,
            text: item.name,
        }

        return itemToSelect
    }

    const mapToList = (items: Array<{ name: string, id: string | number }>) => {
        return items.map(ev => {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
            return <li key={ev.id}>{ev.name}</li>
        })
    }

    React.useEffect(() => {
        videoInfoService.GetAvailableEventsAndGroups()
            .then(userGroupAndEvents => {

                setAllSharingScopes({
                    events: userGroupAndEvents.available.events,
                    groups: userGroupAndEvents.available.groups
                })

                const eventsToSet = new Array<IItemToSelect<string>>()

                setAlreadyAssignedEvents(userGroupAndEvents.assigned.events)
                setAlreadyAssignedGroups(userGroupAndEvents.assigned.groups)

                for (const el of userGroupAndEvents.available.events) {
                    const mapped = mapToItemToSelect(el)
                    eventsToSet.push(mapped)
                }

                setEvents(eventsToSet)

                setAvailableGroupNames(userGroupAndEvents.available.groups.map(r => r.name))
            })
            .catch(e => console.error(e))
    }, [])

    const onGroupSelected = (selectedItem: string, selectedIndex: number) => {
        if (selectedIndex >= 0) {
            setDateSelectorVisible(true)
            setIsSendButtonEnabled(true)
            setSelectedGroup(allSharingScopes.groups[selectedIndex])
        } else {
            setDateSelectorVisible(false)
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

            selectedScopes.forEach((isSelected) => {
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

        let groups: Array<GroupAssigmentModel> | undefined = undefined
        if (selectedGroup) {

            const res = Date.parse(date)

            if (isNaN(res))
            {
                setNotificationMessage('Wprowadzona data jest nieprawidłowa.')
                requestStatusDispatch('receivedFailed')
                return
            }

            groups = [{
                id: selectedGroup.id,
                joinedDate: new Date(Date.parse(date))
            }]
        }

        videoInfoService.SendAssigmentRequest({
            events: events,
            groups: groups
        })
            .then(() => {
                requestStatusDispatch('receivedOk')
            })
            .catch((e) => {
                requestStatusDispatch('receivedFailed')
                console.log(e)
            })
    }

    const [date, setDate] = React.useState("")
    const [dateSelectorVisible, setDateSelectorVisible] = React.useState(false)

    const getDateSelector = () => {
        if (dateSelectorVisible)
            return <div className="field">
                <label className="label">Od kiedy uczęszczasz na zajęcia tej grupy?</label>
                <div className="control">
                    <input className="input" type="date" value={date} onChange={(v) => setDate(v.target.value)} placeholder='20.1.2023' />
                </div>

            </div>
    }

    const getErrorNotification = () => {
        if (requestStatus.wasSend && requestStatus.wasOk == false) {
            return <div className="notification is-danger">
                {notificationMessage}
            </div>
        }
    }

    return (
        <div>
            <div className="content">
                <h2>Wybierz wydarzenia w których brałeś/aś udział i chcesz uzyskać dostęp</h2>
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
                        {getDateSelector()}
                    </div>
                    <div hidden={alreadyAssignedGroups.length == 0}>
                        Masz już dostęp do:
                        <ul>
                            {mapToList(alreadyAssignedGroups)}
                        </ul>
                    </div>
                </div>

            </div>
            <div className="columns">
                <div className="column is-centered has-text-centered">
                    <SelectableList<string>
                        articleClassName='is-info'
                        header='Wydarzenia'
                        onItemStatusChange={eventSelected}
                        text='Wybierz wydarzenia w których brałeś udział!'
                        options={events} />

                    <div hidden={alreadyAssignedEvents.length == 0}>
                        Masz już dostęp do:
                        <ul>
                            {mapToList(alreadyAssignedEvents)}
                        </ul>
                    </div>

                </div>
            </div>

            <div className='columns is-mobile is-centered has-text-centered'>
                <div className='column is-centered'>
                    <Button disabled={!isSendButtonEnabled} classNames='is-large' onClick={() => sendRequest()}>
                        {buttonIcon()}
                    </Button>
                </div>
            </div>

            {getErrorNotification()}

        </div>
    );
}

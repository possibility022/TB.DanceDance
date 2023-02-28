import { faCheck, faCheckSquare, faHouseFloodWater, faSpinner, faUserCheck } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as React from 'react';
import { useState } from 'react';
import videoInfoService from '../../services/VideoInfoService';
import { EventType } from '../../types/EventType';
import { IAssignedEventSharingScopeModel, ISharingScopeModel } from '../../types/SharingScopeModel';
import { Button } from '../Button';
import { Dropdown } from '../Dropdown';
import { IItemToSelect, SelectableList } from './SelectableList';
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
    const [workshops, setWorkshops] = useState<Array<IItemToSelect<string>>>([])

    const [availableGroupNames, setAvailableGroupNames] = useState<Array<string>>([])

    const [isSendButtonEnabled, setIsSendButtonEnabled] = useState(false)
    const [selectedGroup, setSelectedGroup] = useState<ISharingScopeModel>()

    const [allSharingScopes, setAllSharingScopes] = useState<{ events: Array<IAssignedEventSharingScopeModel>, groups: Array<ISharingScopeModel> }>({
        events: [],
        groups: []
    })

    const [selectedScopes] = useState<Map<string, boolean>>(new Map<string, boolean>())

    const mapToItemToSelect = (item: IAssignedEventSharingScopeModel) => {
        const itemToSelect: IItemToSelect<string> = {
            key: item.id,
            text: item.name,
            selectionDisabled: item.isAssigned
        }

        return itemToSelect
    }

    React.useEffect(() => {
        videoInfoService.GetAvailableEventsAndGroups()
            .then(sharringScopes => {

                setAllSharingScopes(sharringScopes)

                const eventsToSet = new Array<IItemToSelect<string>>()
                const workshopsToSet = new Array<IItemToSelect<string>>()

                for (const el of sharringScopes.events) {
                    const mapped = mapToItemToSelect(el)
                    if (el.type == EventType.SmallWorkshop)
                        workshopsToSet.push(mapped)
                    else {
                        eventsToSet.push(mapped)
                    }
                }

                setEvents(eventsToSet)
                setWorkshops(workshopsToSet)

                setAvailableGroupNames(sharringScopes.groups.map(r => r.name))
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
                <h2>Wybierz swoją podstawową grupę do której regularnie uczęszczasz na zajęcia.</h2>
                <h3>Dodatkowo, możesz wybrać wydarzenia na jakich byłeś i gdzie brałeś udział. Wydarzenia np. takie jak Karnawał WestLove, Baltic Swing, Halloween.</h3>
                <p>Wybierz również warsztaty w jakich brałeś udział. Są to mniejsze wydarzenia.</p>
                <p>Pamietaj, że Twój wybór będzie weryfikowany!</p>
            </div>
            <div className='columns'>
                <div className='column is-centered has-text-centered'>
                    <Dropdown isLoading={false}
                        items={availableGroupNames}
                        unselectedText={"Wybierz grupę"}
                        selectedItemIndex={0}
                        onSelected={onGroupSelected}
                        classNames={'is-large'}
                        startWithUnselected={true} />
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
                <div className="column">
                    <SelectableList<string>
                        articleClassName='is-warning'
                        header='Warsztaty'
                        onItemStatusChange={eventSelected}
                        text='Wybierz warsztaty gdzie próbowałaś swoich sił!'
                        options={workshops} />
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

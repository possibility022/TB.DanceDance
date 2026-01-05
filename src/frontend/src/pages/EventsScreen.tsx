import {useEffect, useRef, useState} from 'react';
import {Button} from '../components/Button';
import {CreateNewEvent} from '../components/Events/CreateNewEvent';
import {EventsList} from '../components/Events/EventsList';
import {Event, IEventBase} from '../types/ApiModels/EventsAndGroups';
import videoInfoService from '../services/VideoInfoService';
import {UploadVideoModal} from '../components/Videos/UploadVideoModal';
import SharingWithType from "../types/ApiModels/SharingWithType";

const EventsScreen = () => {

    const [events, setEvents] = useState<Array<Event>>([])
    const [selectedEvent, setSelectedEvent] = useState<Event>()

    useEffect(() => {
        videoInfoService.GetUserEventsAndGroups()
            .then((resp) => {
                setEvents(resp.assigned.events)
            })
            .catch((e) => console.error(e))
    }, [])

    const modalRef = useRef<HTMLDivElement>(null)
    const uploadModalRef = useRef<HTMLDivElement>(null)

    const openModal = () => {
        modalRef.current?.classList.add('is-active')
    }

    const closeModal = () => {
        modalRef.current?.classList.remove('is-active')
    }

    const openUploadModal = (id: string) => {
        setSelectedEvent(events.find((e) => e.id === id))
        uploadModalRef.current?.classList.add('is-active')
    }

    const closeUploadModal = () => {
        uploadModalRef.current?.classList.remove('is-active')
    }

    const onSelected = (id: string) => {
        const event = events.find(r => r.id === id)
        setSelectedEvent(event)
    }

    const submitNewEvent = (newEvent: IEventBase) => {
        videoInfoService.CreateEvent({
            event: newEvent
        })
            .then(added => {
                const newArray = new Array<Event>(...events)
                newArray.push(added.eventObject)
                const sorted = newArray.sort((a, b) => {
                    if (a.date > b.date)
                        return -1
                    else return 1
                })

                setEvents(sorted)
                closeModal()
            })
            .catch(e => console.error(e))
    }

    return (
        <div className='container mt-6'>
            <nav className="level">
                <div className="level-left">
                    <div className="level-item">
                        <p className="subtitle is-5">
                            <Button onClick={openModal}>
                                Utwórz nowe wydarzenie
                            </Button>
                        </p>
                    </div>
                </div>
            </nav>

            <div className="modal" ref={modalRef}>
                <div className="modal-background" onClick={closeModal}></div>
                <div className="modal-content">
                    <CreateNewEvent
                        onCancel={closeModal}
                        onSubmit={submitNewEvent}
                    ></CreateNewEvent>
                </div>
                <button className="modal-close is-large" aria-label="close" onClick={closeModal}></button>
            </div>

            <div className="modal" ref={uploadModalRef}>
                <div className="modal-background" onClick={closeUploadModal}></div>
                <div className="modal-content">
                    <UploadVideoModal
                        sharingWith={SharingWithType.Private}
                        event={selectedEvent}
                    ></UploadVideoModal>
                </div>
                <button className="modal-close is-large" aria-label="close" onClick={closeUploadModal}></button>
            </div>



            <EventsList events={events} onSelected={(id) => onSelected(id)} onUploadClick={openUploadModal}></EventsList>
        </div>
    );
};

export default EventsScreen;

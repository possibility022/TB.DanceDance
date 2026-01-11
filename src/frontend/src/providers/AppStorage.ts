export class AppStorage {
    getAnonymousId() {
        let id = localStorage.getItem('anonymousId')
        if (!id)
            id = this.initializeAnonymousId()

        return id
    }

    private initializeAnonymousId() {
        const id = Math.random().toString(36).substring(2, 15) +
            Math.random().toString(36).substring(2, 15)

        localStorage.setItem('anonymousId',id)

        return id
    }
}

const appStorage = new AppStorage()
export default appStorage

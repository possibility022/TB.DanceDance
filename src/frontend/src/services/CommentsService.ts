import httpApiClient from "./HttpApiClient";
import {CommentResponse, CreateCommentRequest, ListCommentsByLinkResponse, ListCommentsForVideoResponse} from "../types/ApiModels/dancedance/apiModels";
import appStorage from "../providers/AppStorage";
import {AxiosRequestConfig} from "axios";

class CommentsService {

    private getConfigWithAnonymousHeader(): AxiosRequestConfig<any> {
        return {
            headers: {
                "AnonymousId": appStorage.getAnonymousId()
            }
        }
    }

    async getCommentsByLink(linkId: string) {
        const response = await httpApiClient.get<ListCommentsByLinkResponse>(
            `/api/share/${linkId}/comments`,
            this.getConfigWithAnonymousHeader())
        return response.data.comments ?? []
    }

    async getCommentsByVideoId(videoId: string) {
        const response = await httpApiClient.get<ListCommentsForVideoResponse>(`/api/comments/video/${videoId}`)
        return response.data.comments ?? []
    }

    async addCommentAsync(linkId: string, comment: string) {
        const request: CreateCommentRequest = {
            content: comment
        }
        await httpApiClient.post(`/api/share/${linkId}/comments`, request)
    }

    async addCommentAsAnonymousAsync(linkId: string, comment: string, authorName: string) {
        const request: CreateCommentRequest = {
            content: comment,
            authorName: authorName,
            anonymousId: appStorage.getAnonymousId()
        }

        await httpApiClient.post(`/api/share/${linkId}/comments`, request)
    }

    async editCommentAsync(commentId: string, newContent: string, authorName?: string) {
        await httpApiClient.put(`/api/comments/${commentId}`, {
            content: newContent,
            anonymousId: appStorage.getAnonymousId(),
            authorName: authorName
        })
    }

    async deleteCommentAsync(commentId: string) {
        await httpApiClient.delete(`/api/comments/${commentId}`, {
            headers:{
                "AnonymousId": appStorage.getAnonymousId()
            }
        })
    }

    async hideCommentAsync(commentId: string) {
        await httpApiClient.put(`/api/comments/${commentId}/hide`)
    }

    async unHideCommentAsync(commentId: string) {
        await httpApiClient.put(`/api/comments/${commentId}/unhide`)
    }
}

const commentsService = new CommentsService();
export default commentsService;

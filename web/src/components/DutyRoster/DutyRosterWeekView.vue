<template>
    <div>
        <loading-spinner v-if="!isDutyRosterDataMounted" />
        <b-table 
            v-else              
            :items="dutyRosterAssignments" 
            :fields="fields"
            sort-by="assignment"
            small
            head-row-variant="primary"   
            borderless                   
            fixed>
                <template v-slot:table-colgroup>
                    <col style="width:9rem">                            
                </template>
                
                <template v-slot:cell(assignment) ="data"  >
                    <duty-roster-assignment v-on:change="getDutyRosters()" :assignment="data.item" :weekview="true"/>
                </template>

                <template v-slot:head(assignment)="data" >
                    <div style="float: left; margin:0 1rem; padding:0;">
                        <div style="float: left; margin:.15rem .25rem 0  0; font-size:14px">{{data.label}}</div>
                        <b-button
                            variant="success"
                            style="padding:0; height:1rem; width:1rem; margin:auto 0" 
                            @click="addAssignment();"
                            size="sm"> <div style="transform:translate(0,-3px)" >+</div>
                        </b-button>
                    </div>
                </template>

                <template v-slot:head(h0) >
                    <div class="grid796h">
                        <div v-for="i in 7" :key="i" :style="{gridColumnStart: ((i-1)*96)+1,gridColumnEnd:(i*96+2), gridRow:'1/1'}">
                            <div class="h6 text-center">{{getBeautifyTime(i-1)}}</div>
                        </div>
                    </div>
                </template>

                <template v-slot:cell(h0)="data" >
                    <duty-card-week-view v-on:change="getDutyRosters()" :dutyRosterInfo="data.item"/>
                </template>
        </b-table>                
        <b-card><br></b-card>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Watch} from 'vue-property-decorator';

    import DutyCardWeekView from './components/DutyCardWeekView.vue'
    import DutyRosterAssignment from './components/DutyRosterAssignment.vue'

    import moment from 'moment-timezone';

    import { namespace } from "vuex-class";   
    import "@store/modules/CommonInformation";
    const commonState = namespace("CommonInformation");

    import "@store/modules/DutyRosterInformation";   
    const dutyState = namespace("DutyRosterInformation");

    import {localTimeInfoType, locationInfoType } from '../../types/common';
    import { assignmentCardWeekInfoType, attachedDutyInfoType, dutyRangeInfoType, myTeamShiftInfoType, dutiesDetailInfoType} from '../../types/DutyRoster';
    import { shiftInfoType } from '../../types/ShiftSchedule';

    @Component({
        components: {
            DutyCardWeekView,            
            DutyRosterAssignment
        }
    })
    export default class DutyRosterDayView extends Vue {

        @commonState.State
        public location!: locationInfoType;

        // @commonState.State
        // public displayFooter!: boolean;

        @dutyState.State
        public dutyRangeInfo!: dutyRangeInfoType;

        @dutyState.State
        public shiftAvailabilityInfo!: myTeamShiftInfoType[];

        @dutyState.Action
        public UpdateShiftAvailabilityInfo!: (newShiftAvailabilityInfo: myTeamShiftInfoType[]) => void

        isDutyRosterDataMounted = false;

        dutyRosterAssignments: assignmentCardWeekInfoType[] = [];

        dutyRostersJson: attachedDutyInfoType[] = [];
        dutyRosterAssignmentsJson;

        fields =[
            {key:'assignment', label:'Assignments', thClass:' m-0 p-0', tdClass:'p-0 m-0', thStyle:''},
            {key:'h0', label:'', thClass:'', tdClass:'p-0 m-0', thStyle:'margin:0; padding:0;'}
        ]

        dutyColors = [
            {name:'courtroom',  colorCode:'#189fd4'},
            {name:'court',      colorCode:'#189fd4'},
            {name:'jail' ,      colorCode:'#A22BB9'},
            {name:'escort',     colorCode:'#ffb007'},
            {name:'other',      colorCode:'#7a4528'}, 
            {name:'overtime',   colorCode:'#e85a0e'},
            {name:'free',       colorCode:'#e6d9e2'}                        
        ]

        @Watch('location.id', { immediate: true })
        locationChange()
        {
            if (this.isDutyRosterDataMounted) {
                this.getDutyRosters();                                
            }            
        } 

        mounted()
        {
            this.isDutyRosterDataMounted = false;
            console.log('dayview dutyroster mounted')
            this.getDutyRosters()
        }

        public getBeautifyTime(day: number){
            return moment(this.dutyRangeInfo.startDate).add(day, 'days').format('ddd DD MMM YYYY');
        }

        public getDutyRosters(){
            const url = 'api/dutyroster?locationId='+this.location.id+'&start='+this.dutyRangeInfo.startDate+'&end='+this.dutyRangeInfo.endDate;
            this.$http.get(url)
                .then(response => {
                    if(response.data){
                        this.dutyRostersJson = response.data; 
                        console.log(response.data);
                        this.getAssignments();                                                                   
                    }                                   
                })      
        }

        public getAssignments(){
            const url = 'api/assignment?locationId='+this.location.id+'&start='+this.dutyRangeInfo.startDate+'&end='+this.dutyRangeInfo.endDate;
            this.$http.get(url)
                .then(response => {
                    if(response.data){
                        console.log(response.data)
                        this.dutyRosterAssignmentsJson = response.data; 
                        this.getShifts();                             
                    }                                   
                })      
        }

        public getShifts(){
            this.isDutyRosterDataMounted = false;
            const url = 'api/shift?locationId='+this.location.id+'&start='+this.dutyRangeInfo.startDate+'&end='+this.dutyRangeInfo.endDate +'&includeDuties=true';
            this.$http.get(url)
                .then(response => {
                    if(response.data){
                        console.log(response.data)                        
                        this.extractTeamShiftInfo(response.data);                        
                        this.extractAssignmentsInfo(this.dutyRosterAssignmentsJson);                                               
                    }                                   
                })      
        }        

        public extractTeamShiftInfo(shiftsJson){
            this.UpdateShiftAvailabilityInfo([]);
            const allDutySlots: any[] = []
            for(const dutyRoster of this.dutyRostersJson){
                //console.log(dutyRoster)
                const assignmentToThisDuty = this.dutyRosterAssignmentsJson.filter(assignment=>{if(assignment.id==dutyRoster.assignmentId)return true;})[0]
                //console.log(assignmentToThisDuty.lookupCode)
                for(const slot of dutyRoster.dutySlots){
                    slot['color']= this.getType(assignmentToThisDuty.lookupCode.type);
                    slot['type'] = assignmentToThisDuty.lookupCode.type;
                    slot['code'] = assignmentToThisDuty.lookupCode.code;
                    allDutySlots.push(slot)
                }                
            }
            //console.log(allDutySlots)
            for(const shiftJson of shiftsJson)
            {
                //console.log(shiftJson)
                const availabilityInfo = {} as myTeamShiftInfoType;
                const shiftInfo = {} as shiftInfoType;
                shiftInfo.id = shiftJson.id;
                shiftInfo.startDate =  moment(shiftJson.startDate).tz(this.location.timezone).format();
                shiftInfo.endDate = moment(shiftJson.endDate).tz(this.location.timezone).format();
                shiftInfo.timezone = shiftJson.timezone;
                shiftInfo.sheriffId = shiftJson.sheriffId;
                shiftInfo.locationId = shiftJson.locationId;

                const dutySlots = allDutySlots.filter(dutyslot=>{if(dutyslot.sheriffId==shiftInfo.sheriffId && dutyslot.startDate.substring(0,10)==shiftInfo.startDate.substring(0,10))return true})
                const dutiesDetail: dutiesDetailInfoType[] = [];
                for(const duty of dutySlots){
                    //console.log(duty)                    
                    dutiesDetail.push({
                        id:duty.id ,
                        startBin: 0,
                        endBin:0, 
                        startTime:moment(duty.startDate).tz(this.location.timezone).format(),
                        endTime:moment(duty.endDate).tz(this.location.timezone).format(),
                        name: duty.color.name,
                        colorCode: duty.color.colorCode,
                        color: duty.shiftId? duty.color.colorCode: this.dutyColors[5].colorCode,
                        type: duty.type,
                        code: duty.code
                    })
                    //console.log(dutiesDetail)
                }

                const index = this.shiftAvailabilityInfo.findIndex(shift => shift.sheriffId == shiftInfo.sheriffId)
                
                if (index != -1) {
                    this.shiftAvailabilityInfo[index].duties = [];
                    this.shiftAvailabilityInfo[index].availability = [];
                    this.shiftAvailabilityInfo[index].shifts.push(shiftInfo);
                    this.shiftAvailabilityInfo[index].dutiesDetail.push(...dutiesDetail);
                } else {
                    availabilityInfo.shifts = [shiftInfo];
                    availabilityInfo.sheriffId = shiftJson.sheriff.id;
                    availabilityInfo.badgeNumber = shiftJson.sheriff.badgeNumber;
                    availabilityInfo.firstName = shiftJson.sheriff.firstName;
                    availabilityInfo.lastName = shiftJson.sheriff.lastName;
                    availabilityInfo.rank = shiftJson.sheriff.rank;
                    availabilityInfo.availability = [];
                    availabilityInfo.duties = [];
                    availabilityInfo.dutiesDetail = dutiesDetail;
                    this.shiftAvailabilityInfo.push(availabilityInfo);
                }
            }
            this.UpdateShiftAvailabilityInfo(this.shiftAvailabilityInfo);            
        }

        public extractAssignmentsInfo(assignments){
            const dutyWeekDates: string[] = []
            for(let day=0; day<7; day++)
                dutyWeekDates.push(moment(this.dutyRangeInfo.startDate).add(day,'days').format().substring(0,10))                
            //console.log(dutyWeekDates)

            this.dutyRosterAssignments =[]
            let sortOrder = 0;
            for(const assignment of assignments){
                sortOrder++;
                const dutyRostersForThisAssignment: attachedDutyInfoType[] = this.dutyRostersJson.filter(dutyroster=>{if(dutyroster.assignmentId == assignment.id)return true}) 
                //console.log(dutyRostersForThisAssignment)
               
               if(dutyRostersForThisAssignment.length>0){
                    let maximumRow = -2;
                    for(const dutydate of dutyWeekDates){
                        const dutyRostersInOneDay = dutyRostersForThisAssignment.filter(dutyRoster => dutyRoster.startDate.substring(0,10) == dutydate)
                        if(dutyRostersInOneDay.length > maximumRow) maximumRow = dutyRostersInOneDay.length;
                    }

                    const dutyRosterAssignment: assignmentCardWeekInfoType[] = [];

                    for(let row=0; row<maximumRow; row++){
                        dutyRosterAssignment.push({
                            assignment:('00' + sortOrder).slice(-3)+'FTE'+('0'+ row).slice(-2) ,
                            assignmentDetail: assignment,
                            name:assignment.name,
                            code:assignment.lookupCode.code,
                            type: this.getType(assignment.lookupCode.type),
                            0: null,
                            1: null,
                            2: null,
                            3: null,
                            4: null,
                            5: null,
                            6: null,
                            FTEnumber: row,
                            totalFTE: maximumRow
                        })
                    }

                    for(const dutydateInx in dutyWeekDates){
                        const dutyRostersInOneDay = dutyRostersForThisAssignment.filter(dutyRoster => dutyRoster.startDate.substring(0,10) == dutyWeekDates[dutydateInx])
                        for(const dutyRosterInOneDay of dutyRostersInOneDay){
                            for(let row=0; row<maximumRow; row++){
                                //console.log(dutyRosterAssignment[row][dutydateInx])
                                if(!dutyRosterAssignment[row][dutydateInx]){
                                    dutyRosterAssignment[row][dutydateInx] = dutyRosterInOneDay;
                                    break;
                                }
                            }
                        }
                    }
                    // for(const rosterInx in dutyRostersForThisAssignment){
                    //     const dutyRosterForThisAssignment= dutyRostersForThisAssignment[rosterInx];
                    //     console.log(dutyRosterForThisAssignment)
                    //     const index = this.dutyRosterAssignments.findIndex(dutyRoster => dutyRoster.startDate == dutyRosterForThisAssignment.startDate)
                
                    //     // if (index != -1) {
                    //     //     this.dutyRosterAssignments.push({
                    //     //         assignment:('00' + sortOrder).slice(-3)+'FTE'+('0'+ rosterInx).slice(-2) ,
                    //     //         assignmentDetail: assignment,
                    //     //         name:assignment.name,
                    //     //         code:assignment.lookupCode.code,
                    //     //         type: this.getType(assignment.lookupCode.type),
                    //     //         attachedDuty: dutyRosterForThisAssignment,
                    //     //         FTEnumber: Number(rosterInx),
                    //     //         totalFTE: dutyRostersForThisAssignment.length
                    //     //     })
                    //     // }
                    // }
                    this.dutyRosterAssignments.push(...dutyRosterAssignment)
                }else{                
                    this.dutyRosterAssignments.push({
                        assignment:('00' + sortOrder).slice(-3)+'FTE00' ,
                        assignmentDetail: assignment,
                        name:assignment.name,
                        code:assignment.lookupCode.code,
                        type: this.getType(assignment.lookupCode.type),
                        0: null,
                        1: null,
                        2: null,
                        3: null,
                        4: null,
                        5: null,
                        6: null,
                        FTEnumber: 0,
                        totalFTE: 0
                    })
                }
            }

           this.isDutyRosterDataMounted = true;
           this.$emit('dataready');
        }
        
        public getType(type: string){
            for(const color of this.dutyColors){
                if(type.toLowerCase().includes(color.name))return color
            }
            return this.dutyColors[3]
        }

        public fillInArray(array, fillInNum, startBin, endBin){
            return array.map((arr,index) =>{if(index>=startBin && index<endBin) return fillInNum; else return arr;});
        }

        // public addArrays(arrayA, arrayB){
        //     return arrayA.map((arr,index) =>{return arr+arrayB[index]});
        // }

        public unionArrays(arrayA, arrayB){
            return arrayA.map((arr,index) =>{return arr*arrayB[index]});
        }

        public subtractUnionOfArrays(arrayA, arrayB){
            return arrayA.map((arr,index) =>{return arr&&(arrayB[index]>0?0:1)});
        }

        public getTimeRangeBins(startDate, endDate, timezone){
            const startTime = moment(startDate).tz(timezone);
            const endTime = moment(endDate).tz(timezone);
            const startOfDay = moment(startTime).startOf("day");
            const startBin = moment.duration(startTime.diff(startOfDay)).asMinutes()/15;
            const endBin = moment.duration(endTime.diff(startOfDay)).asMinutes()/15;
            return( {startBin: startBin, endBin:endBin } )
        }

        public addAssignment(){ 
            this.$emit('addAssignmentClicked');
        }
        
    }
</script>

<style scoped>   

    .card {
        border: white;
    }

    .gauge {
        direction:rtl;
        position: sticky;
    }

    .grid796h {        
        display:grid;
        grid-template-columns: repeat(672, 0.14881%);
        grid-template-rows: 1.65rem;
        inline-size: 100%;
        font-size: 10px;
        height: 1.58rem;         
    }

    .grid796h > div {      
        padding: 0.3rem 0;
        border: 1px dotted rgb(185, 143, 143);
    }

</style>